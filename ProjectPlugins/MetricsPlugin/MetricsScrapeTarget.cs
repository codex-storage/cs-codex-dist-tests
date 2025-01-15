using Utils;

namespace MetricsPlugin
{
    public interface IHasMetricsScrapeTarget
    {
        Address MetricsScrapeTarget { get; }
    }

    public interface IHasManyMetricScrapeTargets
    {
        Address[] ScrapeTargets { get; }
    }
}
