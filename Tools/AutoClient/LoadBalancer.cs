using Logging;

namespace AutoClient
{
    public class LoadBalancer
    {
        private readonly List<Cdx> instances;
        private readonly object instanceLock = new object();

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
                while (queue.Count > 2)
                {
                    log.Log("Queue full. Waiting...");
                    Thread.Sleep(TimeSpan.FromSeconds(5.0));
                }

                lock (queueLock)
                {
                    queue.Add(action);
                }
            }

            private void Worker()
            {
                while (running)
                {
                    while (queue.Count == 0) Thread.Sleep(TimeSpan.FromSeconds(5.0));

                    Action<CodexWrapper> action = w => { };
                    lock (queueLock)
                    {
                        action = queue[0];
                        queue.RemoveAt(0);
                    }

                    action(instance);
                }
            }
        }

        public LoadBalancer(App app, CodexWrapper[] instances)
        {
            this.instances = instances.Select(i => new Cdx(app, i)).ToList();
        }

        public void Start()
        {
            foreach (var i in instances) i.Start();
        }

        public void Stop()
        {
            foreach (var i in instances) i.Stop();
        }

        public void DispatchOnCodex(Action<CodexWrapper> action)
        {
            lock (instanceLock)
            {
                var i = instances.First();
                instances.RemoveAt(0);
                instances.Add(i);

                i.Queue(action);
            }
        }

        public void DispatchOnSpecificCodex(Action<CodexWrapper> action, string id)
        {
            lock (instanceLock)
            {
                var i = instances.Single(a => a.Id == id);
                instances.Remove(i);
                instances.Add(i);

                i.Queue(action);
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
