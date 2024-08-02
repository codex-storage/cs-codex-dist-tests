using Logging;
using System.Collections.Concurrent;

namespace OverwatchTranscript
{
    public class BucketSet
    {
        private const int numberOfActiveBuckets = 10;
        private readonly ILog log;
        private readonly string workingDir;
        private readonly object _bucketLock = new object();
        private readonly List<EventBucketWriter> fullBuckets = new List<EventBucketWriter>();
        private readonly List<EventBucketWriter> activeBuckets = new List<EventBucketWriter>();
        private readonly ActionQueue queue = new ActionQueue();
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

            queue.Start();
        }

        public void Add(DateTime utc, object payload)
        {
            if (closed) throw new Exception("Buckets already closed!");
            queue.Add(() => AddInternal(utc, payload));
            
            if (queue.Count > 1000)
            {
                Thread.Sleep(1);
            }
        }

        public IFinalizedBucket[] FinalizeBuckets()
        {
            closed = true;
            queue.StopAndJoin();

            if (IsEmpty()) throw new Exception("No entries have been added.");
            if (!string.IsNullOrEmpty(internalErrors)) throw new Exception(internalErrors);

            var buckets = fullBuckets.Concat(activeBuckets).ToArray();
            log.Debug($"Finalizing {buckets.Length} buckets...");

            var finalized = new ConcurrentBag<IFinalizedBucket>();
            var tasks = Parallel.ForEach(buckets, b => finalized.Add(b.FinalizeBucket()));
            if (!tasks.IsCompleted) throw new Exception("Failed to finalize buckets: " + tasks);

            return finalized.ToArray();
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
                activeBuckets.Add(new EventBucketWriter(log, Path.Combine(workingDir, Guid.NewGuid().ToString()), size));
            }
        }
    }
}
