using Core;
using IdentityModel;
using KubernetesWorkflow.Types;
using Logging;
using System.Globalization;

namespace MetricsPlugin
{
    public class MetricsQuery
    {
        private readonly IEndpoint endpoint;
        private readonly ILog log;

        public MetricsQuery(IPluginTools tools, RunningContainer runningContainer)
        {
            RunningContainer = runningContainer;
            log = tools.GetLog();
            var address = RunningContainer.GetAddress(PrometheusContainerRecipe.PortTag);
            endpoint = tools
                .CreateHttp(address.ToString())
                .CreateEndpoint(address, "/api/v1/");
        }

        public RunningContainer RunningContainer { get; }

        public Metrics GetMostRecent(string metricName, IMetricsScrapeTarget target)
        {
            var response = GetLastOverTime(metricName, GetInstanceStringForNode(target));
            if (response == null) throw new Exception($"Failed to get most recent metric: {metricName}");

            var result = new Metrics
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

            Log(target, metricName, result);
            return result;
        }

        public Metrics GetMetrics(string metricName)
        {
            var response = GetAll(metricName);
            if (response == null) throw new Exception($"Failed to get metrics by name: {metricName}");
            var result = MapResponseToMetrics(response);
            Log(metricName, result);
            return result;
        }

        public Metrics GetAllMetricsForNode(IMetricsScrapeTarget target)
        {
            var instanceString = GetInstanceStringForNode(target);
            var response = endpoint.HttpGetJson<PrometheusQueryResponse>($"query?query={instanceString}{GetQueryTimeRange()}");
            if (response.status != "success") throw new Exception($"Failed to get metrics for target: {instanceString}");
            var result = MapResponseToMetrics(response);
            Log(target, result);
            return result;
        }

        private PrometheusQueryResponse? GetLastOverTime(string metricName, string instanceString)
        {
            var response = endpoint.HttpGetJson<PrometheusQueryResponse>($"query?query=last_over_time({metricName}{instanceString}{GetQueryTimeRange()})");
            if (response.status != "success") return null;
            return response;
        }

        private PrometheusQueryResponse? GetAll(string metricName)
        {
            var response = endpoint.HttpGetJson<PrometheusQueryResponse>($"query?query={metricName}{GetQueryTimeRange()}");
            if (response.status != "success") return null;
            return response;
        }

        private Metrics MapResponseToMetrics(PrometheusQueryResponse response)
        {
            return new Metrics
            {
                Sets = response.data.result.Select(CreateMetricsSet).ToArray()
            };
        }

        private MetricsSet CreateMetricsSet(PrometheusQueryResponseDataResultEntry r)
        {
            var result = new MetricsSet
            {
                Name = r.metric.__name__,
                Instance = r.metric.instance,
                Values = MapMultipleValues(r.values)
            };

            if (!string.IsNullOrEmpty(r.metric.file) && !string.IsNullOrEmpty(r.metric.line) && !string.IsNullOrEmpty(r.metric.proc))
            {
                result.AsyncProfiler = new AsyncProfilerMetrics
                {
                    File = r.metric.file,
                    Line = r.metric.line,
                    Proc = r.metric.proc
                };
            }

            return result;
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
                return values.Select(MapValue).ToArray();
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

        private string GetInstanceNameForNode(IMetricsScrapeTarget target)
        {
            return ScrapeTargetHelper.FormatTarget(log, target);
        }

        private string GetInstanceStringForNode(IMetricsScrapeTarget target)
        {
            return "{instance=\"" + GetInstanceNameForNode(target) + "\"}";
        }

        private string GetQueryTimeRange()
        {
            return "[12h]";
        }

        private double ToValue(object v)
        {
            try
            {
                return Convert.ToDouble(v, CultureInfo.InvariantCulture);
            }
            catch
            {
                return double.NaN;
            }
        }

        private DateTime ToTimestamp(object v)
        {
            var unixSeconds = ToValue(v);
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixSeconds);
        }

        private void Log(IMetricsScrapeTarget target, string metricName, Metrics result)
        {
            Log($"{target.Container.Name} '{metricName}' = {result}");
        }

        private void Log(string metricName, Metrics result)
        {
            Log($"'{metricName}' = {result}");
        }

        private void Log(IMetricsScrapeTarget target, Metrics result)
        {
            Log($"{target.Container.Name} => {result}");
        }

        private void Log(string msg)
        {
            log.Log(msg);
        }
    }

    public class Metrics
    {
        public MetricsSet[] Sets { get; set; } = Array.Empty<MetricsSet>();

        public override string ToString()
        {
            return "[" + string.Join(',', Sets.Select(s => s.ToString())) + "]";
        }

        public string AsCsv()
        {
            var allTimestamps = Sets.SelectMany(s => s.Values.Select(v => v.Timestamp)).Distinct().OrderDescending().ToArray();

            var lines = new List<string>();
            MakeLine(lines, e =>
            {
                e.Add("Metrics");
                foreach (var ts in allTimestamps) e.Add(ts.ToEpochTime().ToString());
            });

            foreach (var set in Sets)
            {
                MakeLine(lines, e =>
                {
                    e.Add(set.Name);
                    foreach (var ts in allTimestamps)
                    {
                        var value = set.Values.SingleOrDefault(v => v.Timestamp == ts);
                        if (value == null) e.Add(" ");
                        else e.Add(value.Value.ToString(CultureInfo.InvariantCulture));
                    }
                });
            }

            return string.Join(Environment.NewLine, lines.ToArray());
        }

        private void MakeLine(List<string> lines, Action<List<string>> values)
        {
            var list = new List<string>();
            values(list);
            lines.Add(string.Join(",", list));
        }
    }

    public class MetricsSet
    {
        public string Name { get; set; } = string.Empty;
        public string Instance { get; set; } = string.Empty;
        public AsyncProfilerMetrics? AsyncProfiler { get; set; } = null;
        public MetricsSetValue[] Values { get; set; } = Array.Empty<MetricsSetValue>();

        public override string ToString()
        {
            var prefix = "";
            if (AsyncProfiler != null)
            {
                prefix = $"proc: '{AsyncProfiler.Proc}' in '{AsyncProfiler.File}:{AsyncProfiler.Line}'";
            }

            return $"{prefix}{Name} ({Instance}) : {{{string.Join(",", Values.Select(v => v.ToString()))}}}";
        }
    }

    public class AsyncProfilerMetrics
    {
        public string File { get; set; } = string.Empty;
        public string Line { get; set; } = string.Empty;
        public string Proc { get; set; } = string.Empty;
    }

    public class MetricsSetValue
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }

        public override string ToString()
        {
            return $"<{Timestamp.ToString("o")}={Value}>";
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
        public object[][] values { get; set; } = Array.Empty<object[]>();
    }

    public class ResultEntryMetric
    {
        public string __name__ { get; set; } = string.Empty;
        public string instance { get; set; } = string.Empty;
        public string job { get; set; } = string.Empty;
        // Async profiler output.
        public string? file { get; set; } = null;
        public string? line { get; set; } = null;
        public string? proc { get; set; } = null;
    }

    public class PrometheusAllNamesResponse
    {
        public string status { get; set; } = string.Empty;
        public string[] data { get; set; } = Array.Empty<string>();
    }
}
