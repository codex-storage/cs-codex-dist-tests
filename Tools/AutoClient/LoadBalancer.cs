using Logging;

namespace AutoClient
{
    public class LoadBalancer
    {
        private readonly List<Cdx> instances;
        private readonly object instanceLock = new object();
        private readonly App app;
        private int printDelay = 10;

        private class Cdx
        {
            private readonly ILog log;
            private readonly CodexWrapper instance;
            private readonly List<Action<CodexWrapper>> queue = new List<Action<CodexWrapper>>();
            private readonly object queueLock = new object();
            private bool running = true;
            private Task worker = Task.CompletedTask;

            public Cdx(App app, CodexWrapper instance)
            {
                Id = instance.Node.GetName();
                log = new LogPrefixer(app.Log, $"[Queue-{Id}]");
                this.instance = instance;
            }

            public string Id { get; }
            public int QueueSize => queue.Count;

            public void Start()
            {
                worker = Task.Run(Worker);
            }

            public void Stop()
            {
                running = false;
                worker.Wait();
            }

            public void CheckErrors()
            {
                if (worker.IsFaulted) throw worker.Exception;
            }

            public void Queue(Action<CodexWrapper> action)
            {
                if (queue.Count > 3) Thread.Sleep(TimeSpan.FromSeconds(5.0));
                if (queue.Count > 5) log.Log("Queue full. Waiting...");
                while (queue.Count > 5)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1.0));
                }

                lock (queueLock)
                {
                    queue.Add(action);
                }
            }

            private void Worker()
            {
                try
                {
                    while (running)
                    {
                        while (queue.Count == 0) Thread.Sleep(TimeSpan.FromSeconds(1.0));

                        Action<CodexWrapper> action = w => { };
                        lock (queueLock)
                        {
                            action = queue[0];
                            queue.RemoveAt(0);
                        }

                        action(instance);
                    }
                }
                catch (Exception ex)
                {
                    log.Error("Exception in worker: " + ex);
                    throw;
                }
            }
        }

        private class CdxComparer : IComparer<Cdx>
        {
            public int Compare(Cdx? x, Cdx? y)
            {
                if (x == null || y == null) return 0;
                return x.QueueSize - y.QueueSize;
            }
        }

        public LoadBalancer(App app, CodexWrapper[] instances)
        {
            this.instances = instances.Select(i => new Cdx(app, i)).ToList();
            this.app = app;
        }

        public void Start()
        {
            app.Log.Log("LoadBalancer starting...");
            foreach (var i in instances) i.Start();
        }

        public void Stop()
        {
            app.Log.Log("LoadBalancer stopping...");
            foreach (var i in instances) i.Stop();
        }

        public void DispatchOnCodex(Action<CodexWrapper> action)
        {
            lock (instanceLock)
            {
                instances.Sort(new CdxComparer());
                var i = instances.First();

                i.Queue(action);
            }
            PrintQueue();
        }

        public void DispatchOnSpecificCodex(Action<CodexWrapper> action, string id)
        {
            lock (instanceLock)
            {
                instances.Sort(new CdxComparer());
                var i = instances.Single(a => a.Id == id);

                i.Queue(action);
            }
            PrintQueue();
        }

        private void PrintQueue()
        {
            printDelay--;
            if (printDelay > 0) return;
            printDelay = 10;

            lock (instanceLock)
            {
                foreach (var i in instances)
                {
                    app.Log.Log($"Queue[{i.Id}] = {i.QueueSize} entries");
                }
            }
        }

        public void CheckErrors()
        {
            lock (instanceLock)
            {
                foreach (var i in instances) i.CheckErrors();
            }
        }
    }
}
