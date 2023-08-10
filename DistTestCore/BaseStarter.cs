using Logging;

namespace DistTestCore
{
    public class BaseStarter
    {
        protected readonly TestLifecycle lifecycle;
        private Stopwatch? stopwatch;

        public BaseStarter(TestLifecycle lifecycle)
        {
            this.lifecycle = lifecycle;
        }

        protected void LogStart(string msg)
        {
            Log(msg);
            stopwatch = Stopwatch.Begin(lifecycle.Log, GetClassName());
        }

        protected void LogEnd(string msg)
        {
            stopwatch!.End(msg);
            stopwatch = null;
        }

        protected void Log(string msg)
        {
            lifecycle.Log.Log($"{GetClassName()} {msg}");
        }

        protected void Debug(string msg)
        {
            lifecycle.Log.Debug($"{GetClassName()} {msg}", 1);
        }

        private string GetClassName()
        {
            return $"({GetType().Name})";
        }
    }
}
