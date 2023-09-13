using Core;
using KubernetesWorkflow;
using Logging;

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
            tools.GetLog().Log("Hi from the metrics plugin.");
        }

        public void Decommission()
        {
        }

        public RunningContainers StartMetricsCollector(IMetricsScrapeTarget[] scrapeTargets)
        {
            return starter.CollectMetricsFor(scrapeTargets);
        }

        public MetricsAccess CreateAccessForTarget(RunningContainers runningContainers, IMetricsScrapeTarget target)
        {
            return starter.CreateAccessForTarget(runningContainers, target);
        }

        public LogFile? DownloadAllMetrics(IMetricsAccess metricsAccess, string targetName)
        {
            var downloader = new MetricsDownloader(tools.GetLog());
            return downloader.DownloadAllMetrics(targetName, metricsAccess);
        }
    }
}
