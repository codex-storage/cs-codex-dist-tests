using NUnit.Framework;
using System.Text;

namespace CodexDistTestCore
{
    public class MetricsAggregator
    {
        private readonly TestLog log;
        private readonly K8sManager k8sManager;
        private PrometheusInfo? activePrometheus;

        public MetricsAggregator(TestLog log, K8sManager k8sManager)
        {
            this.log = log;
            this.k8sManager = k8sManager;
        }

        public MetricsAccess BeginCollectingMetricsFor(OnlineCodexNode[] nodes)
        {
            if (activePrometheus != null)
            {
                Assert.Fail("Incorrect test setup: 'GatherMetrics' may be called only once during a test run. Metrics service targets cannot be changed once started. :(");
                throw new InvalidOperationException();
            }

            log.Log($"Starting metrics collecting for {nodes.Length} nodes...");

            var config = GeneratePrometheusConfig(nodes);
            StartPrometheusPod(config);

            log.Log("Metrics service started.");
            return new MetricsAccess(activePrometheus!);
        }

        public void DownloadAllMetrics()
        {
        }

        private void StartPrometheusPod(string config)
        {
            if (activePrometheus != null) return;
            activePrometheus = k8sManager.BringOnlinePrometheus(config);
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
