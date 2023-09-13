using KubernetesWorkflow;

namespace MetricsPlugin
{
    public interface IMetricsScrapeTarget
    {
        string Ip { get; }
        int Port { get; }
    }

    public class MetricsScrapeTarget : IMetricsScrapeTarget
    {
        public MetricsScrapeTarget(string ip, int port)
        {
            Ip = ip;
            Port = port;
        }

        public MetricsScrapeTarget(RunningContainer container, int port)
            : this(container.Pod.PodInfo.Ip, port)
        {
        }

        public MetricsScrapeTarget(RunningContainer container, Port port)
            : this(container, port.Number)
        {
        }

        public string Ip { get; }
        public int Port { get; }
    }
}
