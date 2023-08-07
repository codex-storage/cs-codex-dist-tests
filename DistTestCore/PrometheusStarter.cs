using DistTestCore.Codex;
using DistTestCore.Metrics;
using KubernetesWorkflow;
using System.Text;

namespace DistTestCore
{
    public class PrometheusStarter : BaseStarter
    {
        public PrometheusStarter(TestLifecycle lifecycle, WorkflowCreator workflowCreator)
            : base(lifecycle, workflowCreator)
        {
        }

        public RunningContainers CollectMetricsFor(RunningContainers[] containers)
        {
            LogStart($"Starting metrics server for {containers.Describe()}");
            var startupConfig = new StartupConfig();
            startupConfig.Add(new PrometheusStartupConfig(GeneratePrometheusConfig(containers.Containers())));

            var workflow = workflowCreator.CreateWorkflow();
            var runningContainers = workflow.Start(1, Location.Unspecified, new PrometheusContainerRecipe(), startupConfig);
            if (runningContainers.Containers.Length != 1) throw new InvalidOperationException("Expected only 1 Prometheus container to be created.");

            LogEnd("Metrics server started.");

            return runningContainers;
        }

        private string GeneratePrometheusConfig(RunningContainer[] nodes)
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

            foreach (var node in nodes)
            {
                var ip = node.Pod.PodInfo.Ip;
                var port = node.Recipe.GetPortByTag(CodexContainerRecipe.MetricsPortTag).Number;
                config += $"          - '{ip}:{port}'\n";
            }

            var bytes = Encoding.ASCII.GetBytes(config);
            return Convert.ToBase64String(bytes);
        }
    }
}
