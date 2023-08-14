using DistTestCore.Metrics;
using IdentityModel.Client;
using KubernetesWorkflow;
using Newtonsoft.Json;
using System.Reflection;

namespace DistTestCore
{
    public class GrafanaStarter : BaseStarter
    {
        private const string StorageQuotaThresholdReplaceToken = "\"<CODEX_STORAGEQUOTA>\"";
        private const string BytesUsedGraphAxisSoftMaxReplaceToken = "\"<CODEX_BYTESUSED_SOFTMAX>\"";

        public GrafanaStarter(TestLifecycle lifecycle)
            : base(lifecycle)
        {
        }
        public GrafanaStartInfo StartDashboard(RunningContainer prometheusContainer, CodexSetup codexSetup)
        {
            LogStart($"Starting dashboard server");

            var grafanaContainer = StartGrafanaContainer();
            var grafanaAddress = lifecycle.Configuration.GetAddress(grafanaContainer);

            var http = new Http(lifecycle.Log, new DefaultTimeSet(), grafanaAddress, "api/", AddBasicAuth);

            Log("Connecting datasource...");
            AddDataSource(http, prometheusContainer);

            Log("Uploading dashboard configurations...");
            var jsons = ReadEachDashboardJsonFile(codexSetup);
            var dashboardUrls = jsons.Select(j => UploadDashboard(http, grafanaContainer, j)).ToArray();

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

        private void AddBasicAuth(HttpClient client)
        {
            client.SetBasicAuthentication(
                GrafanaContainerRecipe.DefaultAdminUser,
                GrafanaContainerRecipe.DefaultAdminPassword);
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

        public static string UploadDashboard(Http http, RunningContainer grafanaContainer, string dashboardJson)
        {
            var request = GetDashboardCreateRequest(dashboardJson);
            var response = http.HttpPostString("dashboards/db", request);
            var jsonResponse = JsonConvert.DeserializeObject<GrafanaPostDashboardResponse>(response);
            if (jsonResponse == null || string.IsNullOrEmpty(jsonResponse.url)) throw new Exception("Failed to upload dashboard.");

            var grafanaAddress = grafanaContainer.ClusterExternalAddress;
            return grafanaAddress.Host + ":" + grafanaAddress.Port + jsonResponse.url;
        }

        private static string[] ReadEachDashboardJsonFile(CodexSetup codexSetup)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = new[]
            {
                "DistTestCore.Metrics.dashboard.json"
            };

            return resourceNames.Select(r => GetManifestResource(assembly, r, codexSetup)).ToArray();
        }

        private static string GetManifestResource(Assembly assembly, string resourceName, CodexSetup codexSetup)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) throw new Exception("Unable to find resource " + resourceName);
            using var reader = new StreamReader(stream);
            return ApplyReplacements(reader.ReadToEnd(), codexSetup);
        }

        private static string ApplyReplacements(string input, CodexSetup codexSetup)
        {
            var quotaString = GetQuotaString(codexSetup);
            var softMaxString = GetSoftMaxString(codexSetup);

            return input
                .Replace(StorageQuotaThresholdReplaceToken, quotaString)
                .Replace(BytesUsedGraphAxisSoftMaxReplaceToken, softMaxString);
        }

        private static string GetQuotaString(CodexSetup codexSetup)
        {
            return GetCodexStorageQuotaInBytes(codexSetup).ToString();
        }

        private static string GetSoftMaxString(CodexSetup codexSetup)
        {
            var quota = GetCodexStorageQuotaInBytes(codexSetup);
            var softMax = Convert.ToInt64(quota * 1.1); // + 10%, for nice viewing.
            return softMax.ToString();
        }

        private static long GetCodexStorageQuotaInBytes(CodexSetup codexSetup)
        {
            if (codexSetup.StorageQuota != null) return codexSetup.StorageQuota.SizeInBytes;

            // Codex default: 8GB
            return 8.GB().SizeInBytes;
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
