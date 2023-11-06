using KubernetesWorkflow;
using Utils;

namespace MetricsPlugin
{
    public interface IMetricsScrapeTarget
    {
        string Name { get; }
        Address Address { get; }
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
        public MetricsScrapeTarget(Address address, string name)
        {
            Address = address;
            Name = name;
        }

        public MetricsScrapeTarget(string ip, int port, string name)
            : this(new Address("http://" + ip, port), name)
        {
        }

        public MetricsScrapeTarget(RunningContainer container, string portTag)
            : this(container.GetAddress(portTag), container.Name)
        {
        }

        public string Name { get; }
        public Address Address { get; }
    }
}
