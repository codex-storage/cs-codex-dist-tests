using Core;
using KubernetesWorkflow;

namespace MetricsPlugin
{
    public class MetricsPlugin : IProjectPlugin
    {

        #region IProjectPlugin Implementation

        public void Announce()
        {
            //log.Log("Hi from the metrics plugin.");
        }

        public void Decommission()
        {
        }

        #endregion

        public RunningContainer StartMetricsCollector(RunningContainers[] scrapeTargets)
        {
            return null!;
        }
    }
}
