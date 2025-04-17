using Logging;

namespace BiblioTech
{
    public class CallDispatcher
    {
        private readonly ILog log;
        private readonly object _lock = new object();
        private readonly List<Action> queue = new List<Action>();
        private readonly AutoResetEvent autoResetEvent = new AutoResetEvent(false);

        public CallDispatcher(ILog log)
        {
            this.log = log;
        }

        public void Add(Action call)
        {
            lock (_lock)
            {
                queue.Add(call);
                autoResetEvent.Set();
                if (queue.Count > 100)
                {
                    log.Error("Queue overflow!");
                    queue.Clear();
                }
            }
        }

        public void Start()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        Worker();
                    }
                    catch (Exception ex)
                    {
                        log.Error("Exception in CallDispatcher: " + ex);
                    }
                }
            });
        }

        private void Worker()
        {
            autoResetEvent.WaitOne();
            var tasks = Array.Empty<Action>();

            lock (_lock)
            {
                tasks = queue.ToArray();
                queue.Clear();
            }

            foreach (var task in tasks)
            {
                task();
            }
        }
    }
}
