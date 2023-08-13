using DistTestCore.Metrics;
using KubernetesWorkflow;
using Newtonsoft.Json;
using System.Reflection;
using Utils;

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

            var grafanaContainer = StartGrafanaContainer();
            var grafanaAddress = lifecycle.Configuration.GetAddress(grafanaContainer);

            var http = new Http(lifecycle.Log, new DefaultTimeSet(), grafanaAddress, "api/");

            Log("Connecting datasource...");
            AddDataSource(http, prometheusContainer);

            Log("Uploading dashboard configurations...");
            var jsons = ReadEachDashboardJsonFile();
            var dashboardUrls = jsons.Select(j => UploadDashboard(http, grafanaAddress, j)).ToArray();

            LogEnd("Dashboard server started.");

            return new GrafanaStartInfo(dashboardUrls, grafanaContainer);
        }

        private RunningContainer StartGrafanaContainer()
        {
            var startupConfig = new StartupConfig();

            var workflow = lifecycle.WorkflowCreator.CreateWorkflow();
            var grafanaContainers = workflow.Start(1, Location.Unspecified, new GrafanaContainerRecipe(), startupConfig);
            if (grafanaContainers.Containers.Length != 1) throw new InvalidOperationException("Expected 1 dashboard container to be created.");

            return grafanaContainers.Containers.First();
        }

        private static void AddDataSource(Http http, RunningContainer prometheusContainer)
        {
            var prometheusAddress = prometheusContainer.ClusterExternalAddress;
            var prometheusUrl = prometheusAddress.Host + ":" + prometheusAddress.Port;
            var response = http.HttpPostJson<GrafanaDataSourceRequest, GrafanaDataSourceResponse>("datasources", new GrafanaDataSourceRequest
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

            if (response.message != "Datasource added")
            {
                throw new Exception("Test infra failure: Failed to add datasource to dashboard: " + response.message);
            }
        }

        public static string UploadDashboard(Http http, Address grafanaAddress, string dashboardJson)
        {
            var request = GetDashboardCreateRequest(dashboardJson);
            var response = http.HttpPostString("dashboards/db", request);
            var jsonResponse = JsonConvert.DeserializeObject<GrafanaPostDashboardResponse>(response);
            if (jsonResponse == null || string.IsNullOrEmpty(jsonResponse.url)) throw new Exception("Failed to upload dashboard.");

            return grafanaAddress.Host + ":" + grafanaAddress.Port + jsonResponse.url;
        }

        private static string[] ReadEachDashboardJsonFile()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = new[]
            {
                "DistTestCore.Metrics.dashboard.json"
            };

            return resourceNames.Select(r => GetManifestResource(assembly, r)).ToArray();
        }

        private static string GetManifestResource(Assembly assembly, string resourceName)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) throw new Exception("Unable to find resource " + resourceName);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static string GetDashboardCreateRequest(string dashboardJson)
        {
            return $"{{\"dashboard\": {dashboardJson} ,\"message\": \"Default Codex Dashboard\",\"overwrite\": false}}";
        }
    }

    public class GrafanaStartInfo
    {
        public GrafanaStartInfo(string[] dashboardUrls, RunningContainer container)
        {
            DashboardUrls = dashboardUrls;
            Container = container;
        }

        public string[] DashboardUrls { get; }
        public RunningContainer Container { get; }
    }

    public class GrafanaDataSourceRequest
    {
        public string uid { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
        public string url { get; set; } = string.Empty;
        public string access { get; set; } = string.Empty;
        public bool basicAuth { get; set; }
        public GrafanaDataSourceJsonData jsonData { get; set; } = new();
    }

    public class GrafanaDataSourceResponse
    {
        public int id { get; set; }
        public string message { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
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
