using CodexDistTestCore.Config;

namespace CodexDistTestCore
{
    public interface IMetricsAccess
    {
        int GetMostRecentInt(string metricName, IOnlineCodexNode node);
    }

    public class MetricsAccess : IMetricsAccess
    {
        private readonly K8sCluster k8sCluster = new K8sCluster();
        private readonly Http http;

        public MetricsAccess(PrometheusInfo prometheusInfo)
        {
            http = new Http(
                k8sCluster.GetIp(),
                prometheusInfo.ServicePort,
                "api/v1");
        }

        public int GetMostRecentInt(string metricName, IOnlineCodexNode node)
        {
            var now = DateTime.UtcNow;
            var off = new DateTimeOffset(now);
            var nowUnix = off.ToUnixTimeSeconds();

            var hour = now.AddHours(-1);
            var off2 = new DateTimeOffset(hour);
            var hourUnix = off2.ToUnixTimeSeconds();

            var response = http.HttpGetJson<PrometheusQueryRangeResponse>($"query_range?query=libp2p_peers&start={hourUnix}&end={nowUnix}&step=100");

            return 0;
        }
    }

    public class PrometheusQueryRangeResponse
    {
        public string status { get; set; } = string.Empty;
        public PrometheusQueryRangeResponseData data { get; set; } = new();
    }

    public class PrometheusQueryRangeResponseData
    {
        public string resultType { get; set; } = string.Empty;
        public PrometheusQueryRangeResponseDataResultEntry[] result { get; set; } = Array.Empty<PrometheusQueryRangeResponseDataResultEntry>();
    }

    public class PrometheusQueryRangeResponseDataResultEntry
    {
        public ResultEntryMetric metric { get; set; } = new();
        public ResultEntryValue[] values { get; set; } = Array.Empty<ResultEntryValue>();
    }

    public class ResultEntryMetric
    {
        public string __name__ { get; set; } = string.Empty;
        public string instance { get; set; } = string.Empty;
        public string job { get; set; } = string.Empty;
    }

    public class ResultEntryValue
    {

    }
}
