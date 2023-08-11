using DistTestCore.Codex;
using DistTestCore.Metrics;
using KubernetesWorkflow;
using Logging;
using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using Utils;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

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



            //{
            //    //setup reusable http client
            //    HttpClient client = new HttpClient();
            //    Uri baseUri = new Uri(c.Host + ":" + c.Port);
            //    client.BaseAddress = baseUri;
            //    client.DefaultRequestHeaders.Clear();
            //    client.DefaultRequestHeaders.ConnectionClose = true;

            //    //Post body content
            //    var values = new List<KeyValuePair<string, string>>();
            //    values.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));
            //    var content = new FormUrlEncodedContent(values);

            //    var authenticationString = $"admin:admin";
            //    var base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(authenticationString));

            //    var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/oauth2/token");
            //    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
            //    requestMessage.Content = content;

            //    //make the request
            //    var responsea = Time.Wait(client.SendAsync(requestMessage));
            //    responsea.EnsureSuccessStatusCode();
            //    string responseBody = Time.Wait(responsea.Content.ReadAsStringAsync());
            //    Console.WriteLine(responseBody);

            //}

            //POST / api / datasources HTTP / 1.1
            //Accept: application / json
            //Content - Type: application / json
            //Authorization: Bearer eyJrIjoiT0tTcG1pUlY2RnVKZTFVaDFsNFZXdE9ZWmNrMkZYbk

            var http = new Http(new NullLog(), new DefaultTimeSet(), c, "api/");
            var response = http.HttpPostJson("datasources", new GrafanaDataSource
            {
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


    // [{ "id":1,"uid":"c89eaad3-9184-429f-ac94-8ba0b1824dbb","orgId":1,
    // "name":"Prometheus","type":"prometheus","typeName":"Prometheus",
    // "typeLogoUrl":"public/app/plugins/datasource/prometheus/img/prometheus_logo.svg",
    // "access":"proxy","url":"http://kubernetes.docker.internal:31234","user":"","database":"",
    // "basicAuth":false,"isDefault":true,"jsonData":{ "httpMethod":"POST"},"readOnly":false}]


            var grafanaUrl = c.Host + ":" + c.Port;
            System.Diagnostics.Process.Start("C:\\Users\\Ben\\AppData\\Local\\Programs\\Opera\\opera.exe", grafanaUrl);

            LogEnd("Metrics server started.");



            return runningContainers;
        }

        public class GrafanaDataSource
        {
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
