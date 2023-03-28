using NUnit.Framework;
using System.Text;

namespace CodexDistTestCore
{
    public class MetricsAggregator
    {
        private readonly NumberSource prometheusNumberSource = new NumberSource(0);
        private readonly TestLog log;
        private readonly K8sManager k8sManager;
        private readonly Dictionary<PrometheusInfo, OnlineCodexNode[]> activePrometheuses = new Dictionary<PrometheusInfo, OnlineCodexNode[]>();

        public MetricsAggregator(TestLog log, K8sManager k8sManager)
        {
            this.log = log;
            this.k8sManager = k8sManager;
        }

        public MetricsAccess BeginCollectingMetricsFor(OnlineCodexNode[] nodes)
        {
            var alreadyStartedNodes = nodes.Where(n => activePrometheuses.Values.Any(v => v.Contains(n)));
            if (alreadyStartedNodes.Any())
            {
                Assert.Fail("Incorrect test setup: 'GatherMetrics' was already called on one or more of these OnlineCodexNodes.");
                throw new InvalidOperationException();
            }

            log.Log($"Starting metrics collecting for {nodes.Length} nodes...");

            var config = GeneratePrometheusConfig(nodes);
            var prometheus = k8sManager.BringOnlinePrometheus(config, prometheusNumberSource.GetNextNumber());
            activePrometheuses.Add(prometheus, nodes);

            log.Log("Metrics service started.");
            return new MetricsAccess(prometheus, nodes);
        }

        public void DownloadAllMetrics()
        {
        }

        private string GeneratePrometheusConfig(OnlineCodexNode[] nodes)
        {
            var config = "";
            config += "global:\n";
            config += "  scrape_interval: 30s\n";
            config += "  scrape_timeout: 10s\n";
            config += "\n";
            config += "scrape_configs:\n";
            config += "  - job_name: services\n";
            config += "    metrics_path: /metrics\n";
            config += "    static_configs:\n";
            config += "      - targets:\n";
            config += "          - 'prometheus:9090'\n";

            foreach (var node in nodes)
            {
                var ip = node.Group.PodInfo!.Ip;
                var port = node.Container.MetricsPort;
                config += $"          - '{ip}:{port}'\n";
            }

            var bytes = Encoding.ASCII.GetBytes(config);
            return Convert.ToBase64String(bytes);
        }
    }

    public class PrometheusInfo
    {
        public PrometheusInfo(int servicePort, PodInfo podInfo)
        {
            ServicePort = servicePort;
            PodInfo = podInfo;
        }

        public int ServicePort { get; }
        public PodInfo PodInfo { get; }
    }
}
