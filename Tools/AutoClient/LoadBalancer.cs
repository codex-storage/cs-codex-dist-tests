namespace AutoClient
{
    public class LoadBalancer
    {
        private readonly App app;
        private readonly List<Cdx> instances;
        private readonly object instanceLock = new object();
        private readonly List<Task> tasks = new List<Task>();
        private readonly object taskLock = new object();

        private class Cdx
        {
            public Cdx(CodexWrapper instance)
            {
                Instance = instance;
            }

            public CodexWrapper Instance { get; }
            public bool IsBusy { get; set; } = false;
        }

        public LoadBalancer(App app, CodexWrapper[] instances)
        {
            this.app = app;
            this.instances = instances.Select(i => new Cdx(i)).ToList();
        }

        public void DispatchOnCodex(Action<CodexWrapper> action)
        {
            lock (taskLock)
            {
                WaitUntilNotAllBusy();

                tasks.Add(Task.Run(() => RunTask(action)));
            }
        }

        public void CleanUpTasks()
        {
            lock (taskLock)
            {
                foreach (var task in tasks)
                {
                    if (task.IsFaulted) throw task.Exception;
                }

                tasks.RemoveAll(t => t.IsCompleted);
            }
        }

        private void RunTask(Action<CodexWrapper> action)
        {
            var instance = GetAndSetFreeInstance();
            try
            {
                action(instance.Instance);
            }
            finally
            {
                ReleaseInstance(instance);
            }
        }

        private Cdx GetAndSetFreeInstance()
        {
            lock (instanceLock)
            {
                return GetSetInstance();
            }
        }

        private Cdx GetSetInstance()
        {
            var i = instances.First();
            instances.RemoveAt(0);
            instances.Add(i);

            if (i.IsBusy) return GetSetInstance();

            i.IsBusy = true;
            return i;
        }

        private void ReleaseInstance(Cdx instance)
        {
            lock (instanceLock)
            {
                instance.IsBusy = false;
            }
        }

        private void WaitUntilNotAllBusy()
        {
            if (AllBusy())
            {
                app.Log.Log("[LoadBalancer] All instances are busy. Waiting...");
                while (AllBusy())
                {
                    Thread.Sleep(TimeSpan.FromSeconds(5.0));
                }
            }
        }

        private bool AllBusy()
        {
            lock (instanceLock)
            {
                return instances.All(i => i.IsBusy);
            }
        }
    }
}
