using Newtonsoft.Json;

namespace OverwatchTranscript
{
    public class EventBucket : IFinalizedBucket
    {
        private const int MaxCount = 10000;
        private const int MaxBuffer = 100;

        private readonly object _lock = new object();
        private bool closed = false;

        private readonly string bucketFile;
        private readonly List<EventBucketEntry> buffer = new List<EventBucketEntry>();
        private EventBucketEntry? topEntry;

        public EventBucket(string bucketFile)
        {
            this.bucketFile = bucketFile;
            if (File.Exists(bucketFile)) throw new Exception("Already exists");

            EarliestUtc = DateTime.MaxValue;
            LatestUtc = DateTime.MinValue;
        }

        public int Count { get; private set; }
        public bool IsFull { get; private set; }
        public DateTime EarliestUtc { get; private set; }
        public DateTime LatestUtc { get; private set; }

        public void Add(DateTime utc, object payload)
        {
            lock (_lock)
            {
                if (closed) throw new Exception("Already closed");
                AddToBuffer(utc, payload);
                BufferToFile();
            }
        }

        public IFinalizedBucket FinalizeBucket()
        {
            lock (_lock)
            {
                closed = true;
                BufferToFile();
                SortFileByTimestamps();
            }
            return this;
        }

        public EventBucketEntry? ViewTopEntry()
        {
            if (!closed) throw new Exception("Bucket not closed yet. FinalizeBucket first.");
            return topEntry;
        }

        public void PopTopEntry()
        {
            var lines = File.ReadAllLines(bucketFile).ToList();
            lines.RemoveAt(0);
            File.WriteAllLines(bucketFile, lines);

            if (lines.Any())
            {
                topEntry = JsonConvert.DeserializeObject<EventBucketEntry>(lines[0]);
            }
            else
            {
                topEntry = null;
            }
        }

        private void AddToBuffer(DateTime utc, object payload)
        {  
            var typeName = payload.GetType().FullName;
            if (string.IsNullOrEmpty(typeName)) throw new Exception("Empty typename for payload");
            if (utc == default) throw new Exception("DateTimeUtc not set");

            var entry = new EventBucketEntry
            {
                Utc = utc,
                Event = new OverwatchEvent
                {
                    Type = typeName,
                    Payload = JsonConvert.SerializeObject(payload)
                }
            };

            if (utc < EarliestUtc) EarliestUtc = utc;
            if (utc > LatestUtc) LatestUtc = utc;
            Count++;
            IsFull = Count > MaxCount;

            buffer.Add(entry);
        }

        private void BufferToFile()
        {
            if (buffer.Count > MaxBuffer)
            {
                using var file = File.Open(bucketFile, FileMode.Append);
                using var writer = new StreamWriter(file);
                foreach (var entry in buffer)
                {
                    writer.WriteLine(JsonConvert.SerializeObject(entry));
                }
                buffer.Clear();
            }
        }

        private void SortFileByTimestamps()
        {
            var lines = File.ReadAllLines(bucketFile);
            var entries = lines.Select(JsonConvert.DeserializeObject<EventBucketEntry>)
                .Cast<EventBucketEntry>()
                .OrderBy(e => e.Utc)
                .ToArray();

            File.Delete(bucketFile);
            File.WriteAllLines(bucketFile, entries.Select(JsonConvert.SerializeObject));

            topEntry = entries.First();
        }
    }

    public interface IFinalizedBucket
    {
        int Count { get; }
        bool IsFull { get; }
        DateTime EarliestUtc { get; }
        DateTime LatestUtc { get; }
        EventBucketEntry? ViewTopEntry();
        void PopTopEntry();
    }

    [Serializable]
    public class EventBucketEntry
    {
        public DateTime Utc { get; set; }
        public OverwatchEvent Event { get; set; } = new();
    }
}
