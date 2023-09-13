using Core;
using KubernetesWorkflow;
using Logging;

namespace MetricsPlugin
{
    public class MetricsPlugin : IProjectPlugin, IHasLogPrefix, IHasMetadata
    {
        private readonly IPluginTools tools;
        private readonly PrometheusStarter starter;

        public MetricsPlugin(IPluginTools tools)
        {
            this.tools = tools;
            starter = new PrometheusStarter(tools);
        }

        public string LogPrefix => "(Metrics) ";

        public void Announce()
        {
            tools.GetLog().Log($"Prometheus plugin loaded with '{starter.GetPrometheusId()}'.");
        }

        public void AddMetadata(IAddMetadata metadata)
        {
            metadata.Add("prometheusid", starter.GetPrometheusId());
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
