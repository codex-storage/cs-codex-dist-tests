using KubernetesWorkflow;

namespace MetricsPlugin
{
    public interface IMetricsScrapeTarget
    {
        RunningContainer Container { get; }
        string MetricsPortTag { get; }
    }

    public interface IHasMetricsScrapeTarget
    {
        IMetricsScrapeTarget MetricsScrapeTarget { get; }
    }

    public interface IHasManyMetricScrapeTargets
    {
        IMetricsScrapeTarget[] ScrapeTargets { get; }
    }

    public class MetricsScrapeTarget : IMetricsScrapeTarget
    {
        public MetricsScrapeTarget(RunningContainer container, string metricsPortTag)
        {
            Container = container;
            MetricsPortTag = metricsPortTag;
        }

        public RunningContainer Container { get; }
        public string MetricsPortTag { get; }
    }
}
