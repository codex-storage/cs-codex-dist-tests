using Logging;
using Newtonsoft.Json;

namespace OverwatchTranscript
{
    public interface IFinalizedBucket
    {
        bool IsEmpty { get; }
        DateTime? SeeTopUtc();
        BucketTop? TakeTop();
    }

    public class BucketTop
    {
        public BucketTop(DateTime utc, OverwatchEvent[] events)
        {
            Utc = utc;
            Events = events;
        }

        public DateTime Utc { get; }
        public OverwatchEvent[] Events { get; }
    }

    public class EventBucketReader : IFinalizedBucket
    {
        private readonly object topLock = new object();
        private readonly string bucketFile;
        private readonly AutoResetEvent topReadySignal = new AutoResetEvent(false);
        private readonly AutoResetEvent topTakenSignal = new AutoResetEvent(true);
        private BucketTop? top;
        private DateTime? topUtc;

        public EventBucketReader(ILog log, string bucketFile)
        {
            this.bucketFile = bucketFile;
            if (!File.Exists(bucketFile)) throw new Exception("Doesn't exist: " + bucketFile);

            log.Debug("Read Bucket open: " + bucketFile);

            Task.Run(ReadBucket);
        }
        
        public bool IsEmpty { get; private set; }

        public DateTime? SeeTopUtc()
        {
            if (IsEmpty) return null;
            return topUtc;
        }

        public BucketTop? TakeTop()
        {
            if (IsEmpty) return null;
            topReadySignal.WaitOne();

            lock (topLock)
            {
                var t = top;
                top = null;
                topUtc = null;
                topTakenSignal.Set();
                return t;
            }
        }

        private void ReadBucket()
        {
            using var file = File.OpenRead(bucketFile);
            using var reader = new StreamReader(file);

            while (true)
            {
                topTakenSignal.WaitOne(10);
                lock (topLock)
                {
                    if (top == null)
                    {
                        top = CreateNewTop(reader);
                        if (top != null)
                        {
                            topUtc = top.Utc;
                        }
                        topReadySignal.Set();
                    }
                    if (top == null)
                    {
                        IsEmpty = true;
                        return;
                    }
                }
            }
        }

        private EventBucketEntry? nextEntry = null;
        private BucketTop? CreateNewTop(StreamReader reader)
        {
            if (nextEntry == null)
            {
                nextEntry = ReadEntry(reader);
                if (nextEntry == null) return null;
            }

            var topEntry = nextEntry;
            var entries = new List<EventBucketEntry>
            {
                topEntry
            };

            nextEntry = ReadEntry(reader);
            while (nextEntry != null && nextEntry.Utc == topEntry.Utc)
            {
                entries.Add(nextEntry);
                nextEntry = ReadEntry(reader);
            }

            return new BucketTop(topEntry.Utc, entries.Select(e => e.Event).ToArray());
        }

        private EventBucketEntry? ReadEntry(StreamReader reader)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrEmpty(line)) return null;
            return JsonConvert.DeserializeObject<EventBucketEntry>(line);
        }
    }
}
