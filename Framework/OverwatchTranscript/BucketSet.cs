using Logging;

namespace OverwatchTranscript
{
    public class BucketSet
    {
        private const int numberOfActiveBuckets = 10;
        private readonly object queueLock = new object();
        private List<Action> queue = new List<Action>();
        private readonly Task queueWorker;
        private readonly ILog log;
        private readonly string workingDir;
        private readonly object _bucketLock = new object();
        private readonly List<EventBucket> fullBuckets = new List<EventBucket>();
        private readonly List<EventBucket> activeBuckets = new List<EventBucket>();
        private int activeBucketIndex = 0;
        private bool closed = false;
        private string internalErrors = string.Empty;
        
        public BucketSet(ILog log, string workingDir)
        {
            this.log = log;
            this.workingDir = workingDir;

            for (var i = 0; i < numberOfActiveBuckets;i++)
            {
                AddNewBucket();
            }

            queueWorker = Task.Run(QueueWorker);
        }

        public void Add(DateTime utc, object payload)
        {
            if (closed) throw new Exception("Buckets already closed!");
            int count = 0;
            lock (queueLock)
            {
                queue.Add(() => AddInternal(utc, payload));
                count = queue.Count;
            }

            if (count > 1000)
            {
                Thread.Sleep(1);
            }
        }

        public IFinalizedBucket[] FinalizeBuckets()
        {
            closed = true;
            WaitForZeroQueue();
            queueWorker.Wait();

            if (IsEmpty()) throw new Exception("No entries have been added.");
            if (!string.IsNullOrEmpty(internalErrors)) throw new Exception(internalErrors);

            var buckets = fullBuckets.Concat(activeBuckets).ToArray();
            log.Debug($"Finalizing {buckets.Length} buckets...");
            return buckets.Select(b => b.FinalizeBucket()).ToArray();
        }

        private bool IsEmpty()
        {
            return fullBuckets.All(b => b.Count == 0) && activeBuckets.All(b => b.Count == 0);
        }

        private void AddInternal(DateTime utc, object payload)
        {
            try
            {
                lock (_bucketLock)
                {
                    var current = activeBuckets[activeBucketIndex];
                    current.Add(utc, payload);
                    activeBucketIndex = (activeBucketIndex + 1) % numberOfActiveBuckets;

                    if (current.IsFull)
                    {
                        log.Debug("Bucket is full. New bucket...");
                        fullBuckets.Add(current);
                        activeBuckets.Remove(current);
                        AddNewBucket();
                    }
                }
            }
            catch (Exception ex)
            {
                internalErrors += ex.ToString();
                log.Error(ex.ToString());
            }
        }

        private static int bucketSizeIndex = 0;
        private static int[] bucketSizes = new[]
        {
            10000,
            15000,
            20000,
        };

        private void AddNewBucket()
        {
            lock (_bucketLock)
            {
                var size = bucketSizes[bucketSizeIndex];
                bucketSizeIndex = (bucketSizeIndex + 1) % bucketSizes.Length;
                activeBuckets.Add(new EventBucket(log, Path.Combine(workingDir, Guid.NewGuid().ToString()), size));
            }
        }

        private void QueueWorker()
        {
            while (true)
            {
                List<Action> work = null!;
                lock (queueLock)
                {
                    work = queue;
                    queue = new List<Action>();
                }

                if (closed && !work.Any()) return;
                foreach (var action in work)
                {
                    action();
                }

                Thread.Sleep(0);
            }
        }

        private void WaitForZeroQueue()
        {
            log.Debug("Wait for zero pending.");
            while (true)
            {
                lock (queueLock)
                {
                    log.Debug("(wait) Pending: " + queue.Count);
                    if (queue.Count == 0) return;
                }
                Thread.Sleep(10);
            }
        }
    }
}
