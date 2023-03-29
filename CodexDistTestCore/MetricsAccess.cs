using CodexDistTestCore.Config;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace CodexDistTestCore
{
    public interface IMetricsAccess
    {
        void AssertThat(IOnlineCodexNode node, string metricName, IResolveConstraint constraint, string message = "");
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

        public void AssertThat(IOnlineCodexNode node, string metricName, IResolveConstraint constraint, string message = "")
        {
            var metricValue = GetMetricWithTimeout(metricName, node);
            Assert.That(metricValue, constraint, message);
        }

        private double GetMetricWithTimeout(string metricName, IOnlineCodexNode node)
        {
            var start = DateTime.UtcNow;

            while (true)
            {
                var mostRecent = GetMostRecent(metricName, node);
                if (mostRecent != null) return Convert.ToDouble(mostRecent);
                if (DateTime.UtcNow - start > Timing.WaitForMetricTimeout())
                {
                    Assert.Fail($"Timeout: Unable to get metric '{metricName}'.");
                    throw new TimeoutException();
                }

                Utils.Sleep(TimeSpan.FromSeconds(2));
            }
        }

        private object? GetMostRecent(string metricName, IOnlineCodexNode node)
        {
            var n = (OnlineCodexNode)node;
            CollectionAssert.Contains(nodes, n, "Incorrect test setup: Attempt to get metrics for OnlineCodexNode from the wrong MetricsAccess object. " +
                "(This CodexNode is tracked by a different instance.)");

            var response = GetMetric(metricName);
            if (response == null) return null;

            var value = GetValueFromResponse(n, response);
            if (value == null) return null;
            if (value.Length != 2) throw new InvalidOperationException("Expected value to be [double, string].");
            return value[1];
        }

        private PrometheusQueryResponse? GetMetric(string metricName)
        {
            var response = http.HttpGetJson<PrometheusQueryResponse>($"query?query=last_over_time({metricName}[12h])");
            if (response.status != "success") return null;
            return response;
        }

        private object[]? GetValueFromResponse(OnlineCodexNode node, PrometheusQueryResponse response)
        {
            var pod = node.Group.PodInfo!;
            var forNode = response.data.result.SingleOrDefault(d => d.metric.instance == $"{pod.Ip}:{node.Container.MetricsPort}");
            if (forNode == null) return null;
            if (forNode.value == null || forNode.value.Length == 0) return null;
            return forNode.value;
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
