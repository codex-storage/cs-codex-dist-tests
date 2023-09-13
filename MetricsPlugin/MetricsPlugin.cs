using Core;
using KubernetesWorkflow;

namespace MetricsPlugin
{
    public class MetricsPlugin : IProjectPlugin
    {
        private readonly IPluginTools tools;
        private readonly PrometheusStarter starter;

        public MetricsPlugin(IPluginTools tools)
        {
            this.tools = tools;
            starter = new PrometheusStarter(tools);
        }


        public void Announce()
        {
            //log.Log("Hi from the metrics plugin.");
        }

        public void Decommission()
        {
        }

        public RunningContainers StartMetricsCollector(RunningContainers[] scrapeTargets)
        {
            return starter.CollectMetricsFor(scrapeTargets);
        }
    }
}
