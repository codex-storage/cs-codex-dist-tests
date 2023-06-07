using KubernetesWorkflow;
using Logging;

namespace DistTestCore
{
    public class BaseStarter
    {
        protected readonly TestLifecycle lifecycle;
        protected readonly WorkflowCreator workflowCreator;
        private Stopwatch? stopwatch;

        public BaseStarter(TestLifecycle lifecycle, WorkflowCreator workflowCreator)
        {
            this.lifecycle = lifecycle;
            this.workflowCreator = workflowCreator;
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
