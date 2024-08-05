namespace OverwatchTranscript
{
    public class ActionQueue
    {
        // Using ConcurrentQueue<> here would make this process slower.
        private readonly object queueLock = new object();
        private readonly AutoResetEvent signal = new AutoResetEvent(false);
        private List<Action> queue = new List<Action>();
        private Task queueWorker = null!;
        private bool stopping = false;

        public void Start()
        {
            queueWorker = Task.Run(QueueWorker);
        }

        public int Count { get; private set; }

        public void StopAndJoin()
        {
            stopping = true;
            queueWorker.Wait();
            if (queue.Count > 0) throw new Exception("not all acions handled");
            queueWorker.Dispose();
        }

        public void Add(Action action)
        {
            if (stopping) throw new Exception("queue stopping");

            lock (queueLock)
            {
                queue.Add(action);
                Count = queue.Count;
            }
            signal.Set();
        }

        private void QueueWorker()
        {
            while (true)
            {
                signal.WaitOne(10);

                List<Action> work = null!;
                lock (queueLock)
                {
                    work = queue;
                    queue = new List<Action>();
                    Count = 0;
                }
                if (stopping && !work.Any()) return;
                
                foreach (var action in work)
                {
                    action();
                }
            }
        }
    }
}
