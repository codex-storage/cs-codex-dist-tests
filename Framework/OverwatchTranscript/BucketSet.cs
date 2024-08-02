using Logging;

namespace OverwatchTranscript
{
    public class BucketSet
    {
        private const int numberOfActiveBuckets = 5;
        private readonly object _counterLock = new object();
        private int pendingAdds = 0;
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
        }

        public void Add(DateTime utc, object payload)
        {
            if (closed) throw new Exception("Buckets already closed!");
            AddPending();
            Task.Run(() => AddInternal(utc, payload));
        }

        public IFinalizedBucket[] FinalizeBuckets()
        {
            closed = true;
            WaitForZeroPending();

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
                        fullBuckets.Add(current);
                        activeBuckets.Remove(current);
                        AddNewBucket();
                    }
                    RemovePending();
                }
            }
            catch (Exception ex)
            {
                internalErrors += ex.ToString();
            }
        }

        private void AddNewBucket()
        {
            lock (_bucketLock)
            {
                activeBuckets.Add(new EventBucket(log, Path.Combine(workingDir, Guid.NewGuid().ToString())));
            }
        }

        private void AddPending()
        {
            lock (_counterLock)
            {
                pendingAdds++;
                log.Debug("(+) Pending: " + pendingAdds);
            }
        }

        private void RemovePending()
        {
            lock (_counterLock)
            {
                pendingAdds--;
                if (pendingAdds < 0) internalErrors += "Pending less than zero";
                log.Debug("(-) Pending: " + pendingAdds);
            }
        }

        private void WaitForZeroPending()
        {
            log.Debug("Wait for zero pending.");
            while (true)
            {
                lock (_counterLock)
                {
                    log.Debug("(wait) Pending: " + pendingAdds);
                    if (pendingAdds == 0) return;
                }
                Thread.Sleep(10);
            }
        }
    }
}
