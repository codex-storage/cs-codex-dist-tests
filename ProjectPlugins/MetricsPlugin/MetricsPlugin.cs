using Core;
using KubernetesWorkflow.Types;
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

        public RunningPod DeployMetricsCollector(IMetricsScrapeTarget[] scrapeTargets)
        {
            return starter.CollectMetricsFor(scrapeTargets);
        }

        public IMetricsAccess WrapMetricsCollectorDeployment(RunningPod runningPod, IMetricsScrapeTarget target)
        {
            runningPod = SerializeGate.Gate(runningPod);
            return starter.CreateAccessForTarget(runningPod, target);
        }

        public LogFile? DownloadAllMetrics(IMetricsAccess metricsAccess, string targetName)
        {
            var downloader = new MetricsDownloader(tools.GetLog());
            return downloader.DownloadAllMetrics(targetName, metricsAccess);
        }
    }
}
