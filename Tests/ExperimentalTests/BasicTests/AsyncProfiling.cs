using NUnit.Framework;
using MetricsPlugin;
using Utils;

namespace CodexTests.BasicTests
{
    [TestFixture]
    public class AsyncProfiling : CodexDistTest
    {
        [Test]
        public void AsyncProfileMetricsPlz()
        {
            var node = StartCodex(s => s.EnableMetrics());
            var metrics = Ci.GetMetricsFor(scrapeInterval: TimeSpan.FromSeconds(3.0), node).Single();

            var file = GenerateTestFile(100.MB());
            node.UploadFile(file);

            Thread.Sleep(10000);

            var profilerMetrics = new AsyncProfileMetrics(metrics.GetAllMetrics());

            var log = GetTestLog();
            log.Log($"{nameof(profilerMetrics.CallCount)} = {profilerMetrics.CallCount.Highest()}");
            log.Log($"{nameof(profilerMetrics.ExecTime)} = {profilerMetrics.ExecTime.Highest()}");
            log.Log($"{nameof(profilerMetrics.ExecTimeWithChildren)} = {profilerMetrics.ExecTimeWithChildren.Highest()}");
            log.Log($"{nameof(profilerMetrics.SingleExecTimeMax)} = {profilerMetrics.SingleExecTimeMax.Highest()}");
            log.Log($"{nameof(profilerMetrics.WallTime)} = {profilerMetrics.WallTime.Highest()}");
        }
    }

    public class AsyncProfileMetrics
    {
        public AsyncProfileMetrics(Metrics metrics)
        {
            CallCount = CreateMetric(metrics, "chronos_call_count_total");
            ExecTime = CreateMetric(metrics, "chronos_exec_time_total");
            ExecTimeWithChildren = CreateMetric(metrics, "chronos_exec_time_with_children_total");
            SingleExecTimeMax = CreateMetric(metrics, "chronos_single_exec_time_max");
            WallTime = CreateMetric(metrics, "chronos_wall_time_total");
        }

        public AsyncProfileMetric CallCount { get; }
        public AsyncProfileMetric ExecTime { get; }
        public AsyncProfileMetric ExecTimeWithChildren { get; }
        public AsyncProfileMetric SingleExecTimeMax { get; }
        public AsyncProfileMetric WallTime { get; }

        private static AsyncProfileMetric CreateMetric(Metrics metrics, string name)
        {
            var sets = metrics.Sets.Where(s => s.Name == name).ToArray();
            return new AsyncProfileMetric(sets);
        }
    }

    public class AsyncProfileMetric
    {
        private readonly MetricsSet[] metricsSets;

        public AsyncProfileMetric(MetricsSet[] metricsSets)
        {
            this.metricsSets = metricsSets;
        }

        public MetricsSet Highest()
        {
            MetricsSet? result = null;
            var highest = double.MinValue;
            foreach (var metric in metricsSets)
            {
                foreach (var value in metric.Values)
                {
                    if (value.Value > highest)
                    {
                        highest = value.Value;
                        result = metric;
                    }
                }
            }

            if (result == null) throw new Exception("None were highest");
            return result;
        }
    }
}
