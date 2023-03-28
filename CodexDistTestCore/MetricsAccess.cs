using CodexDistTestCore.Config;
using NUnit.Framework;

namespace CodexDistTestCore
{
    public interface IMetricsAccess
    {
        int? GetMostRecentInt(string metricName, IOnlineCodexNode node);
    }

    public class MetricsAccess : IMetricsAccess
    {
        private readonly K8sCluster k8sCluster = new K8sCluster();
        private readonly Http http;
        private readonly OnlineCodexNode[] nodes;

        public MetricsAccess(PrometheusInfo prometheusInfo, OnlineCodexNode[] nodes)
        {
            http = new Http(
                k8sCluster.GetIp(),
                prometheusInfo.ServicePort,
                "api/v1");
            this.nodes = nodes;
        }

        public int? GetMostRecentInt(string metricName, IOnlineCodexNode node)
        {
            var n = (OnlineCodexNode)node;
            CollectionAssert.Contains(nodes, n, "Incorrect test setup: Attempt to get metrics for OnlineCodexNode from the wrong MetricsAccess object. " +
                "(This CodexNode is tracked by a different instance.)");

            var pod = n.Group.PodInfo!;

            var response = http.HttpGetJson<PrometheusQueryResponse>($"query?query=last_over_time({metricName}[12h])");
            if (response.status != "success") return null;

            var forNode = response.data.result.SingleOrDefault(d => d.metric.instance == $"{pod.Ip}:{n.Container.MetricsPort}");
            if (forNode == null) return null;

            if (forNode.value == null || forNode.value.Length == 0) return null;

            if (forNode.value.Length != 2) throw new InvalidOperationException("Expected value to be [double, string].");
            // [0] = double, timestamp
            // [1] = string, value

            return Convert.ToInt32(forNode.value[1]);
        }
    }

    public class PrometheusQueryResponse
    {
        public string status { get; set; } = string.Empty;
        public PrometheusQueryResponseData data { get; set; } = new();
    }

    public class PrometheusQueryResponseData
    {
        public string resultType { get; set; } = string.Empty;
        public PrometheusQueryResponseDataResultEntry[] result { get; set; } = Array.Empty<PrometheusQueryResponseDataResultEntry>();
    }

    public class PrometheusQueryResponseDataResultEntry
    {
        public ResultEntryMetric metric { get; set; } = new();
        public object[] value { get; set; } = Array.Empty<object>();
    }

    public class ResultEntryMetric
    {
        public string __name__ { get; set; } = string.Empty;
        public string instance { get; set; } = string.Empty;
        public string job { get; set; } = string.Empty;
    }
}
