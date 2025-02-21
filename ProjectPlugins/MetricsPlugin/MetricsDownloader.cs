﻿using Logging;
using System.Globalization;

namespace MetricsPlugin
{
    public class MetricsDownloader
    {
        private readonly ILog log;

        public MetricsDownloader(ILog log)
        {
            this.log = log;
        }

        public LogFile? DownloadAllMetrics(string targetName, IMetricsAccess access)
        {
            var metrics = access.GetAllMetrics();
            if (metrics == null || metrics.Sets.Length == 0 || metrics.Sets.All(s => s.Values.Length == 0)) return null;

            var headers = new[] { "timestamp" }.Concat(metrics.Sets.Select(s => s.Name)).ToArray();
            var map = CreateValueMap(metrics);

            return WriteToFile(targetName, headers, map);
        }

        private LogFile WriteToFile(string nodeName, string[] headers, Dictionary<DateTime, List<string>> map)
        {
            var file = log.CreateSubfile("csv");
            log.Log($"Downloading metrics for {nodeName} to file {file.Filename}");

            file.WriteRaw(string.Join(",", headers));

            foreach (var pair in map)
            {
                file.WriteRaw(string.Join(",", new[] { FormatTimestamp(pair.Key) }.Concat(pair.Value)));
            }

            return file;
        }

        private Dictionary<DateTime, List<string>> CreateValueMap(Metrics metrics)
        {
            var map = CreateForAllTimestamps(metrics);
            foreach (var metric in metrics.Sets)
            {
                AddToMap(map, metric);
            }
            return map;

        }

        private Dictionary<DateTime, List<string>> CreateForAllTimestamps(Metrics metrics)
        {
            var result = new Dictionary<DateTime, List<string>>();
            var timestamps = metrics.Sets.SelectMany(s => s.Values).Select(v => v.Timestamp).Distinct().ToArray();
            foreach (var timestamp in timestamps) result.Add(timestamp, new List<string>());
            return result;
        }

        private void AddToMap(Dictionary<DateTime, List<string>> map, MetricsSet metric)
        {
            foreach (var key in map.Keys)
            {
                map[key].Add(GetValueAtTimestamp(key, metric));
            }
        }

        private string GetValueAtTimestamp(DateTime key, MetricsSet metric)
        {
            var value = metric.Values.SingleOrDefault(v => v.Timestamp == key);
            if (value == null) return "";
            return value.Value.ToString(CultureInfo.InvariantCulture);
        }

        private string FormatTimestamp(DateTime key)
        {
            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var diff = key - origin;
            return Math.Floor(diff.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }
    }
}
