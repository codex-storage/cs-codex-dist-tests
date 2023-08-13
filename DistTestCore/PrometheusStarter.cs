using DistTestCore.Codex;
using DistTestCore.Metrics;
using KubernetesWorkflow;
using Logging;
using Newtonsoft.Json;
using System.Reflection;
using System.Text;

namespace DistTestCore
{
    public class PrometheusStarter : BaseStarter
    {
        public PrometheusStarter(TestLifecycle lifecycle)
            : base(lifecycle)
        {
        }

        public RunningContainers CollectMetricsFor(RunningContainers[] containers)
        {
            LogStart($"Starting metrics server for {containers.Describe()}");
            var startupConfig = new StartupConfig();
            startupConfig.Add(new PrometheusStartupConfig(GeneratePrometheusConfig(containers.Containers())));

            var workflow = lifecycle.WorkflowCreator.CreateWorkflow();
            var runningContainers = workflow.Start(1, Location.Unspecified, new PrometheusContainerRecipe(), startupConfig);
            if (runningContainers.Containers.Length != 1) throw new InvalidOperationException("Expected only 1 Prometheus container to be created.");

            var pc = runningContainers.Containers.First().ClusterExternalAddress;
            var prometheusUrl = pc.Host + ":" + pc.Port; 

            workflow = lifecycle.WorkflowCreator.CreateWorkflow();
            var grafanaContainers = workflow.Start(1, Location.Unspecified, new GrafanaContainerRecipe(), startupConfig);
            if (grafanaContainers.Containers.Length != 1) throw new InvalidOperationException("should be 1");

            Thread.Sleep(3000);

            var c = grafanaContainers.Containers.First().ClusterExternalAddress;

            var http = new Http(new NullLog(), new DefaultTimeSet(), c, "api/");
            var response = http.HttpPostJson("datasources", new GrafanaDataSource
            {
                uid = "c89eaad3-9184-429f-ac94-8ba0b1824dbb",
                name = "CodexPrometheus",
                type = "prometheus",
                url = prometheusUrl,
                access = "proxy",
                basicAuth = false,
                jsonData = new GrafanaDataSourceJsonData
                {
                    httpMethod = "POST"
                }
            });

            var response2 = http.HttpPostString("dashboards/db", GetDashboardJson());
            var jsonResponse = JsonConvert.DeserializeObject<GrafanaPostDashboardResponse>(response2);

            var grafanaUrl = c.Host + ":" + c.Port + jsonResponse.url;
            System.Diagnostics.Process.Start("C:\\Users\\Ben\\AppData\\Local\\Programs\\Opera\\opera.exe", grafanaUrl);

            LogEnd("Metrics server started.");

            return runningContainers;
        }

        public class GrafanaDataSource
        {
            public string uid { get; set; } = string.Empty;
            public string name { get; set; } = string.Empty;
            public string type { get; set; } = string.Empty;
            public string url { get; set; } = string.Empty;
            public string access { get; set; } = string.Empty;
            public bool basicAuth { get; set; }
            public GrafanaDataSourceJsonData jsonData { get; set; } = new();
        }

        public class GrafanaDataSourceJsonData
        {
            public string httpMethod { get; set; } = string.Empty;
        }

        public class GrafanaPostDashboardResponse
        {
            public int id { get; set; }
            public string slug { get; set; } = string.Empty;
            public string status { get; set; } = string.Empty;
            public string uid { get; set; } = string.Empty;
            public string url { get; set; } = string.Empty;
            public int version { get; set; }
        }

        private string GetDashboardJson()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "DistTestCore.Metrics.dashboard.json";

            //var names = assembly.GetManifestResourceNames();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                var dashboard = reader.ReadToEnd();

                return $"{{\"dashboard\": {dashboard} ,\"message\": \"Default Codex Dashboard\",\"overwrite\": false}}";
            }
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
