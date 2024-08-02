using Logging;
using Newtonsoft.Json;

namespace OverwatchTranscript
{
    public class EventBucketWriter
    {
        private const int MaxBuffer = 1000;

        private readonly object _lock = new object();
        private bool closed = false;
        private readonly ILog log;
        private readonly string bucketFile;
        private readonly int maxCount;
        private readonly List<EventBucketEntry> buffer = new List<EventBucketEntry>();

        public EventBucketWriter(ILog log, string bucketFile, int maxCount)
        {
            this.log = log;
            this.bucketFile = bucketFile;
            this.maxCount = maxCount;
            if (File.Exists(bucketFile)) throw new Exception("Already exists");

            log.Debug("Write Bucket open: " + bucketFile);
        }

        public int Count { get; private set; }
        public bool IsFull { get; private set; }

        public void Add(DateTime utc, object payload)
        {
            lock (_lock)
            {
                if (closed) throw new Exception("Already closed");
                AddToBuffer(utc, payload);
                BufferToFile(emptyBuffer: false);
            }
        }

        public IFinalizedBucket FinalizeBucket()
        {
            lock (_lock)
            {
                closed = true;
                BufferToFile(emptyBuffer: true);
                SortFileByTimestamps();
            }
            log.Debug($"Finalized bucket with {Count} entries");
            return new EventBucketReader(log, bucketFile);
        }

        public override string ToString()
        {
            return $"EventBucket: " + Count;
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

            Count++;
            IsFull = Count > maxCount;

            buffer.Add(entry);
        }

        private void BufferToFile(bool emptyBuffer)
        {
            if (emptyBuffer || buffer.Count > MaxBuffer)
            {
                using var file = File.Open(bucketFile, FileMode.Append);
                using var writer = new StreamWriter(file);
                foreach (var entry in buffer)
                {
                    writer.WriteLine(JsonConvert.SerializeObject(entry));
                }
                log.Debug($"Bucket wrote {buffer.Count} entries to file.");
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
        }
    }

    [Serializable]
    public class EventBucketEntry
    {
        public DateTime Utc { get; set; }
        public OverwatchEvent Event { get; set; } = new();
    }
}
