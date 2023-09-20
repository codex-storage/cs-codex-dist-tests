using KubernetesWorkflow;

namespace MetricsPlugin
{
    public interface IMetricsScrapeTarget
    {
        string Name { get; }
        string Ip { get; }
        int Port { get; }
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
        public MetricsScrapeTarget(string ip, int port, string name)
        {
            Ip = ip;
            Port = port;
            Name = name;
        }

        public MetricsScrapeTarget(RunningContainer container, int port)
            : this(container.Pod.PodInfo.Ip, port, container.Name)
        {
        }

        public MetricsScrapeTarget(RunningContainer container, Port port)
            : this(container, port.Number)
        {
        }

        public string Name { get; }
        public string Ip { get; }
        public int Port { get; }
    }
}
