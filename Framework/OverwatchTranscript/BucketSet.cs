namespace OverwatchTranscript
{
    public class BucketSet
    {
        private const int numberOfActiveBuckets = 5;
        private readonly object _counterLock = new object();
        private int pendingAdds = 0;

        private readonly object _bucketLock = new object();
        private readonly List<EventBucket> fullBuckets = new List<EventBucket>();
        private readonly List<EventBucket> activeBuckets = new List<EventBucket>();
        private int activeBucketIndex = 0;
        private bool closed = false;
        private readonly string workingDir;

        public BucketSet(string workingDir)
        {
            this.workingDir = workingDir;

            for (var i = 0; i < numberOfActiveBuckets;i++)
            {
                AddNewBucket();
            }
        }

        public string Error { get; private set; } = string.Empty;

        public void Add(DateTime utc, object payload)
        {
            if (closed) throw new Exception("Buckets already closed!");
            AddPending();
            Task.Run(() => AddInternal(utc, payload));
        }

        public bool IsEmpty()
        {
            return fullBuckets.All(b => b.Count == 0) && activeBuckets.All(b => b.Count == 0);
        }

        public IFinalizedBucket[] FinalizeBuckets()
        {
            closed = true;
            WaitForZeroPending();

            var buckets = fullBuckets.Concat(activeBuckets).ToArray();
            return buckets.Select(b => b.FinalizeBucket()).ToArray();
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
                Error += ex.ToString();
            }
        }

        private void AddNewBucket()
        {
            lock (_bucketLock)
            {
                activeBuckets.Add(new EventBucket(Path.Combine(workingDir, Guid.NewGuid().ToString())));
            }
        }

        private void AddPending()
        {
            lock (_counterLock)
            {
                pendingAdds++;
            }
        }

        private void RemovePending()
        {
            lock (_counterLock)
            {
                pendingAdds--;
                if (pendingAdds < 0) Error += "Pending less than zero";
            }
        }

        private void WaitForZeroPending()
        {
            while (true)
            {
                lock (_counterLock)
                {
                    if (pendingAdds == 0) return;
                }
                Thread.Sleep(10);
            }
        }
    }
}
