using Logging;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace OverwatchTranscript
{
    public interface IFinalizedBucket
    {
        bool IsEmpty { get; }
        void Update();
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
        private readonly string bucketFile;
        private readonly ConcurrentQueue<BucketTop> topQueue = new ConcurrentQueue<BucketTop>();
        private readonly AutoResetEvent itemDequeued = new AutoResetEvent(false);
        private readonly AutoResetEvent itemEnqueued = new AutoResetEvent(false);
        private bool sourceIsEmpty;

        public EventBucketReader(ILog log, string bucketFile)
        {
            this.bucketFile = bucketFile;
            if (!File.Exists(bucketFile)) throw new Exception("Doesn't exist: " + bucketFile);

            log.Debug("Read Bucket open: " + bucketFile);

            Task.Run(ReadBucket);
        }
        
        public bool IsEmpty { get; private set; }

        public void Update()
        {
            if (IsEmpty) return;
            while (topQueue.Count == 0)
            {
                UpdateIsEmpty();
                if (IsEmpty) return;

                itemDequeued.Set();
                itemEnqueued.WaitOne(200);
            }
        }

        public DateTime? SeeTopUtc()
        {
            if (IsEmpty) return null;
            if (topQueue.TryPeek(out BucketTop? top))
            {
                return top.Utc;
            }
            return null;
        }

        public BucketTop? TakeTop()
        {
            if (IsEmpty) return null;
            if (topQueue.TryDequeue(out BucketTop? top))
            {
                itemDequeued.Set();
                return top;
            }
            return null;
        }

        private void ReadBucket()
        {
            using var file = File.OpenRead(bucketFile);
            using var reader = new StreamReader(file);

            while (true)
            {
                while (topQueue.Count < 5)
                {
                    var top = CreateNewTop(reader);
                    if (top != null)
                    {
                        topQueue.Enqueue(top);
                        itemEnqueued.Set();
                    }
                    else
                    {
                        sourceIsEmpty = true;
                        UpdateIsEmpty();
                        return;
                    }
                }

                itemDequeued.Reset();
                itemDequeued.WaitOne(5000);
            }
        }

        private void UpdateIsEmpty()
        {
            var allEmpty = sourceIsEmpty && topQueue.IsEmpty;
            if (!IsEmpty && allEmpty)
            {
                File.Delete(bucketFile);
                IsEmpty = true;
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
