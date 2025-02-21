using Core;
using KubernetesWorkflow.Types;
using Logging;
using Utils;

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

        public RunningPod DeployMetricsCollector(Address[] scrapeTargets, TimeSpan scrapeInterval)
        {
            return starter.CollectMetricsFor(scrapeTargets, scrapeInterval);
        }

        public IMetricsAccess WrapMetricsCollectorDeployment(RunningPod runningPod, Address target)
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
