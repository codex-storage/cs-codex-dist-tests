﻿using CodexDistTestCore.Config;
using System.Globalization;

namespace CodexDistTestCore
{
    public class MetricsQuery
    {
        private readonly K8sCluster k8sCluster = new K8sCluster();
        private readonly Http http;

        public MetricsQuery(PrometheusInfo prometheusInfo)
        {
            http = new Http(
                k8sCluster.GetIp(),
                prometheusInfo.ServicePort,
                "api/v1");
        }

        public Metrics? GetMostRecent(string metricName, OnlineCodexNode node)
        {
            var response = GetLastOverTime(metricName, GetInstanceStringForNode(node));
            if (response == null) return null;

            return new Metrics
            {
                Sets = response.data.result.Select(r =>
                {
                    return new MetricsSet
                    {
                        Instance = r.metric.instance,
                        Values = MapSingleValue(r.value)
                    };
                }).ToArray()
            };
        }

        public Metrics? GetMetrics(string metricName)
        {
            var response = GetAll(metricName);
            if (response == null) return null;
            return MapResponseToMetrics(response);
        }

        public Metrics? GetAllMetricsForNode(OnlineCodexNode node)
        {
            var response = http.HttpGetJson<PrometheusQueryResponse>($"query?query={GetInstanceStringForNode(node)}{GetQueryTimeRange()}");
            if (response.status != "success") return null;
            return MapResponseToMetrics(response);
        }

        private PrometheusQueryResponse? GetLastOverTime(string metricName, string instanceString)
        {
            var response = http.HttpGetJson<PrometheusQueryResponse>($"query?query=last_over_time({metricName}{instanceString}{GetQueryTimeRange()})");
            if (response.status != "success") return null;
            return response;
        }

        private PrometheusQueryResponse? GetAll(string metricName)
        {
            var response = http.HttpGetJson<PrometheusQueryResponse>($"query?query={metricName}{GetQueryTimeRange()}");
            if (response.status != "success") return null;
            return response;
        }

        private Metrics MapResponseToMetrics(PrometheusQueryResponse response)
        {
            return new Metrics
            {
                Sets = response.data.result.Select(r =>
                {
                    return new MetricsSet
                    {
                        Name = r.metric.__name__,
                        Instance = r.metric.instance,
                        Values = MapMultipleValues(r.values)
                    };
                }).ToArray()
            };
        }

        private MetricsSetValue[] MapSingleValue(object[] value)
        {
            if (value != null && value.Length > 0)
            {
                return new[]
                {
                    MapValue(value)
                };
            }
            return Array.Empty<MetricsSetValue>();
        }

        private MetricsSetValue[] MapMultipleValues(object[][] values)
        {
            if (values != null && values.Length > 0)
            {
                return values.Select(v => MapValue(v)).ToArray();
            }
            return Array.Empty<MetricsSetValue>();
        }

        private MetricsSetValue MapValue(object[] value)
        {
            if (value.Length != 2) throw new InvalidOperationException("Expected value to be [double, string].");

            return new MetricsSetValue
            {
                Timestamp = ToTimestamp(value[0]),
                Value = ToValue(value[1])
            };            
        }

        private string GetInstanceNameForNode(OnlineCodexNode node)
        {
            var pod = node.Group.PodInfo!;
            return $"{pod.Ip}:{node.Container.MetricsPort}";
        }

        private string GetInstanceStringForNode(OnlineCodexNode node)
        {
            return "{instance=\"" + GetInstanceNameForNode(node) + "\"}";
        }

        private string GetQueryTimeRange()
        {
            return "[12h]";
        }

        private double ToValue(object v)
        {
            return Convert.ToDouble(v, CultureInfo.InvariantCulture);
        }

        private DateTime ToTimestamp(object v)
        {
            var unixSeconds = ToValue(v);
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixSeconds);
        }
    }

    public class Metrics
    {
        public MetricsSet[] Sets { get; set; } = Array.Empty<MetricsSet>();
    }

    public class MetricsSet
    {
        public string Name { get; set; } = string.Empty;
        public string Instance { get; set; } = string.Empty;
        public MetricsSetValue[] Values { get; set; } = Array.Empty<MetricsSetValue>();
    }

    public class MetricsSetValue
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
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
        public object[][] values { get; set; } = Array.Empty<object[]>();
    }

    public class ResultEntryMetric
    {
        public string __name__ { get; set; } = string.Empty;
        public string instance { get; set; } = string.Empty;
        public string job { get; set; } = string.Empty;
    }

    public class PrometheusAllNamesResponse
    {
        public string status { get; set; } = string.Empty;
        public string[] data { get; set; } = Array.Empty<string>();
    }
}