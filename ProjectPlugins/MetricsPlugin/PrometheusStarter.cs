using Core;
using KubernetesWorkflow;
using KubernetesWorkflow.Types;
using Logging;
using System.Text;
using Utils;

namespace MetricsPlugin
{
    public class PrometheusStarter
    {
        private readonly PrometheusContainerRecipe recipe = new PrometheusContainerRecipe();
        private readonly IPluginTools tools;

        public PrometheusStarter(IPluginTools tools)
        {
            this.tools = tools;
        }

        public RunningPod CollectMetricsFor(Address[] targets, TimeSpan scrapeInterval)
        {
            if (!targets.Any()) throw new ArgumentException(nameof(targets) + " must not be empty.");

            Log($"Starting metrics server for {targets.Length} targets...");
            var startupConfig = new StartupConfig();
            startupConfig.Add(new PrometheusStartupConfig(GeneratePrometheusConfig(targets, scrapeInterval)));

            var workflow = tools.CreateWorkflow();
            var runningContainers = workflow.Start(1, recipe, startupConfig).WaitForOnline();
            if (runningContainers.Containers.Length != 1) throw new InvalidOperationException("Expected only 1 Prometheus container to be created.");

            Log("Metrics server started.");
            return runningContainers;
        }

        public MetricsAccess CreateAccessForTarget(RunningPod metricsPod, Address target)
        {
            var metricsQuery = new MetricsQuery(tools, metricsPod.Containers.Single());
            return new MetricsAccess(metricsQuery, target);
        }

        public string GetPrometheusId()
        {
            return recipe.Image;
        }

        private void Log(string msg)
        {
            tools.GetLog().Log(msg);
        }

        private string GeneratePrometheusConfig(Address[] targets, TimeSpan scrapeInterval)
        {
            var secs = Convert.ToInt32(scrapeInterval.TotalSeconds);
            if (secs < 1) throw new Exception("ScrapeInterval can't be < 1s");
            if (secs > 60) throw new Exception("ScrapeInterval can't be > 60s");

            var config = "";
            config += "global:\n";
            config += $"  scrape_interval: {secs}s\n";
            config += $"  scrape_timeout: {secs}s\n";
            config += "\n";
            config += "scrape_configs:\n";
            config += "  - job_name: services\n";
            config += "    metrics_path: /metrics\n";
            config += "    static_configs:\n";
            config += "      - targets:\n";

            foreach (var target in targets)
            {
                config += $"          - '{FormatTarget(target)}'\n";
            }

            var bytes = Encoding.ASCII.GetBytes(config);
            return Convert.ToBase64String(bytes);
        }

        private string FormatTarget(Address target)
        {
            return ScrapeTargetHelper.FormatTarget(target);
        }
    }

    public static class ScrapeTargetHelper
    {
        public static string FormatTarget(Address target)
        {
            var host = target.Host.Replace("http://", "").Replace("https://", "");
            return $"{host}:{target.Port}";
        }
    }
}
