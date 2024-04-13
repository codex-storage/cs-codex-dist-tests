using Core;
using KubernetesWorkflow.Types;
using Logging;

namespace MetricsPlugin
{
    public static class CoreInterfaceExtensions
    {
        public static RunningPod DeployMetricsCollector(this CoreInterface ci, params IHasMetricsScrapeTarget[] scrapeTargets)
        {
            return Plugin(ci).DeployMetricsCollector(scrapeTargets.Select(t => t.MetricsScrapeTarget).ToArray());
        }

        public static RunningPod DeployMetricsCollector(this CoreInterface ci, params IMetricsScrapeTarget[] scrapeTargets)
        {
            return Plugin(ci).DeployMetricsCollector(scrapeTargets);
        }

        public static IMetricsAccess WrapMetricsCollector(this CoreInterface ci, RunningPod metricsPod, IHasMetricsScrapeTarget scrapeTarget)
        {
            return ci.WrapMetricsCollector(metricsPod, scrapeTarget.MetricsScrapeTarget);
        }

        public static IMetricsAccess WrapMetricsCollector(this CoreInterface ci, RunningPod metricsPod, IMetricsScrapeTarget scrapeTarget)
        {
            return Plugin(ci).WrapMetricsCollectorDeployment(metricsPod, scrapeTarget);
        }

        public static IMetricsAccess[] GetMetricsFor(this CoreInterface ci, params IHasManyMetricScrapeTargets[] manyScrapeTargets)
        {
            return ci.GetMetricsFor(manyScrapeTargets.SelectMany(t => t.ScrapeTargets).ToArray());
        }

        public static IMetricsAccess[] GetMetricsFor(this CoreInterface ci, params IHasMetricsScrapeTarget[] scrapeTargets)
        {
            return ci.GetMetricsFor(scrapeTargets.Select(t => t.MetricsScrapeTarget).ToArray());
        }

        public static IMetricsAccess[] GetMetricsFor(this CoreInterface ci, params IMetricsScrapeTarget[] scrapeTargets)
        {
            var rc = ci.DeployMetricsCollector(scrapeTargets);
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
