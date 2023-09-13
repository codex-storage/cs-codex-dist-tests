using Core;
using KubernetesWorkflow;
using Logging;

namespace MetricsPlugin
{
    public class MetricsPlugin : IProjectPlugin
    {

        #region IProjectPlugin Implementation

        public void Announce(ILog log)
        {
            log.Log("Hi from the metrics plugin.");
        }

        public void Initialize(IPluginTools tools)
        {
        }

        public void Finalize(ILog log)
        {
        }

        #endregion

        public RunningContainer StartMetricsCollector(RunningContainers[] scrapeTargets)
        {
            return null!;
        }
    }
}
