using DistTestCore.Metrics;
using KubernetesWorkflow;
using Logging;
using Newtonsoft.Json;
using System.Reflection;

namespace DistTestCore
{
    public class GrafanaStarter : BaseStarter
    {
        public GrafanaStarter(TestLifecycle lifecycle)
            : base(lifecycle)
        {
        }
        public GrafanaStartInfo StartDashboard(RunningContainer prometheusContainer)
        {
            LogStart($"Starting dashboard server");
            var startupConfig = new StartupConfig();
            
            var pc = prometheusContainer.ClusterExternalAddress;
            var prometheusUrl = pc.Host + ":" + pc.Port;

            var workflow = lifecycle.WorkflowCreator.CreateWorkflow();
            var grafanaContainers = workflow.Start(1, Location.Unspecified, new GrafanaContainerRecipe(), startupConfig);
            if (grafanaContainers.Containers.Length != 1) throw new InvalidOperationException("Expected 1 dashboard container to be created.");

            //Thread.Sleep(3000);

            var grafanaContainer = grafanaContainers.Containers.First();
            var c = grafanaContainer.ClusterExternalAddress;

            var http = new Http(new NullLog(), new DefaultTimeSet(), c, "api/");

            // {"datasource":{"id":1,"uid":"c89eaad3-9184-429f-ac94-8ba0b1824dbb","orgId":1,"name":"CodexPrometheus","type":"prometheus","typeLogoUrl":"","access":"proxy","url":"http://kubernetes.docker.internal:31971","user":"","database":"","basicAuth":false,"basicAuthUser":"","withCredentials":false,"isDefault":false,"jsonData":{"httpMethod":"POST"},"secureJsonFields":{},"version":1,"readOnly":false},"id":1,"message":"Datasource added","name":"CodexPrometheus"}
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

            return new GrafanaStartInfo(grafanaUrl, grafanaContainer);
        }

        private string GetDashboardJson()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "DistTestCore.Metrics.dashboard.json";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                var dashboard = reader.ReadToEnd();

                return $"{{\"dashboard\": {dashboard} ,\"message\": \"Default Codex Dashboard\",\"overwrite\": false}}";
            }
        }
    }

    public class GrafanaStartInfo
    {
        public GrafanaStartInfo(string dashboardUrl, RunningContainer container)
        {
            DashboardUrl = dashboardUrl;
            Container = container;
        }

        public string DashboardUrl { get; }
        public RunningContainer Container { get; }
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

}
