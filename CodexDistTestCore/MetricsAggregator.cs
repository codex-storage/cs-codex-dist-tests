using NUnit.Framework;

namespace CodexDistTestCore
{
    public class MetricsAggregator
    {
        private readonly TestLog log;
        private readonly K8sManager k8sManager;
        private readonly List<OnlineCodexNode> activeMetricsNodes = new List<OnlineCodexNode>();
        private PrometheusInfo? activePrometheus;

        public MetricsAggregator(TestLog log, K8sManager k8sManager)
        {
            this.log = log;
            this.k8sManager = k8sManager;
        }

        public MetricsAccess BeginCollectingMetricsFor(IOnlineCodexNode[] nodes)
        {
            EnsurePrometheusPod();

            AddNewCodexNodes(nodes);

            // Get IPS and ports from all nodes, format prometheus configuration
            var config = GeneratePrometheusConfig();
            // Create config file inside prometheus pod
            k8sManager.UploadFileToPod(
                activePrometheus!.PodInfo.Name,
                K8sPrometheusSpecs.ContainerName,
                config,
                K8sPrometheusSpecs.ConfigFilepath);

            // HTTP POST request to the /-/reload endpoint (when the --web.enable-lifecycle flag is enabled). 

            return new MetricsAccess();
        }

        public void DownloadAllMetrics()
        {
        }

        private void EnsurePrometheusPod()
        {
            if (activePrometheus != null) return;
            activePrometheus = k8sManager.BringOnlinePrometheus();
        }

        private void AddNewCodexNodes(IOnlineCodexNode[] nodes)
        {
            activeMetricsNodes.AddRange(nodes.Where(n => !activeMetricsNodes.Contains(n)).Cast<OnlineCodexNode>());
        }

        private Stream GeneratePrometheusConfig()
        {
            var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);

            writer.WriteLine("global:");
            writer.WriteLine("  scrape_interval: 30s");
            writer.WriteLine("  scrape_timeout: 10s");
            writer.WriteLine("");
            writer.WriteLine("rule_files:");
            writer.WriteLine("  - alert.yml");
            writer.WriteLine("");
            writer.WriteLine("scrape_configs:");
            writer.WriteLine("  - job_name: services");
            writer.WriteLine("    metrics_path: /metrics");
            writer.WriteLine("    static_configs:");
            writer.WriteLine("      - targets:");
            writer.WriteLine("          - 'prometheus:9090'");

            foreach (var node in activeMetricsNodes)
            {
                var ip = node.Group.PodInfo!.Ip;
                var port = node.Container.ServicePort;
                writer.WriteLine($"          - '{ip}:{port}'");
            }

            return stream;
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
