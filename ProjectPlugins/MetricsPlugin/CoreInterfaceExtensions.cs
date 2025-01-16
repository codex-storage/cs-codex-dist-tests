using Core;
using KubernetesWorkflow.Types;
using Logging;
using Utils;

namespace MetricsPlugin
{
    public static class CoreInterfaceExtensions
    {
        public static RunningPod DeployMetricsCollector(this CoreInterface ci, TimeSpan scrapeInterval, params IHasMetricsScrapeTarget[] scrapeTargets)
        {
            return Plugin(ci).DeployMetricsCollector(scrapeTargets.Select(t => t.GetMetricsScrapeTarget()).ToArray(), scrapeInterval);
        }

        public static RunningPod DeployMetricsCollector(this CoreInterface ci, TimeSpan scrapeInterval, params Address[] scrapeTargets)
        {
            return Plugin(ci).DeployMetricsCollector(scrapeTargets, scrapeInterval);
        }

        public static IMetricsAccess WrapMetricsCollector(this CoreInterface ci, RunningPod metricsPod, IHasMetricsScrapeTarget scrapeTarget)
        {
            return ci.WrapMetricsCollector(metricsPod, scrapeTarget.GetMetricsScrapeTarget());
        }

        public static IMetricsAccess WrapMetricsCollector(this CoreInterface ci, RunningPod metricsPod, Address scrapeTarget)
        {
            return Plugin(ci).WrapMetricsCollectorDeployment(metricsPod, scrapeTarget);
        }

        public static IMetricsAccess[] GetMetricsFor(this CoreInterface ci, TimeSpan scrapeInterval, params IHasManyMetricScrapeTargets[] manyScrapeTargets)
        {
            return ci.GetMetricsFor(scrapeInterval, manyScrapeTargets.SelectMany(t => t.GetMetricsScrapeTargets()).ToArray());
        }

        public static IMetricsAccess[] GetMetricsFor(this CoreInterface ci, TimeSpan scrapeInterval, params IHasMetricsScrapeTarget[] scrapeTargets)
        {
            return ci.GetMetricsFor(scrapeInterval, scrapeTargets.Select(t => t.GetMetricsScrapeTarget()).ToArray());
        }

        public static IMetricsAccess[] GetMetricsFor(this CoreInterface ci, TimeSpan scrapeInterval, params Address[] scrapeTargets)
        {
            var rc = ci.DeployMetricsCollector(scrapeInterval, scrapeTargets);
            return scrapeTargets.Select(t => ci.WrapMetricsCollector(rc, t)).ToArray();
        }

        public static LogFile? DownloadAllMetrics(this CoreInterface ci, IMetricsAccess metricsAccess, string targetName)
        {
            return Plugin(ci).DownloadAllMetrics(metricsAccess, targetName);
        }

        private static MetricsPlugin Plugin(CoreInterface ci)
        {
            return ci.GetPlugin<MetricsPlugin>();
        }
    }
}
