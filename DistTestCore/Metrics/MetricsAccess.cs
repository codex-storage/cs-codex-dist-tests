using KubernetesWorkflow;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Utils;

namespace DistTestCore.Metrics
{
    public interface IMetricsAccess
    {
        void AssertThat(string metricName, IResolveConstraint constraint, string message = "");
    }

    public class MetricsUnavailable : IMetricsAccess
    {
        public void AssertThat(string metricName, IResolveConstraint constraint, string message = "")
        {
            Assert.Fail("Incorrect test setup: Metrics were not enabled for this group of Codex nodes. Add 'EnableMetrics()' after 'SetupCodexNodes()' to enable it.");
            throw new InvalidOperationException();
        }
    }

    public class MetricsAccess : IMetricsAccess
    {
        private readonly MetricsQuery query;
        private readonly RunningContainer node;

        public MetricsAccess(MetricsQuery query, RunningContainer node)
        {
            this.query = query;
            this.node = node;
        }

        public void AssertThat(string metricName, IResolveConstraint constraint, string message = "")
        {
            var metricSet = GetMetricWithTimeout(metricName);
            var metricValue = metricSet.Values[0].Value;
            Assert.That(metricValue, constraint, message);
        }

        public Metrics? GetAllMetrics()
        {
            return query.GetAllMetricsForNode(node);
        }

        private MetricsSet GetMetricWithTimeout(string metricName)
        {
            var start = DateTime.UtcNow;

            while (true)
            {
                var mostRecent = GetMostRecent(metricName);
                if (mostRecent != null) return mostRecent;
                if (DateTime.UtcNow - start > Timing.WaitForMetricTimeout())
                {
                    Assert.Fail($"Timeout: Unable to get metric '{metricName}'.");
                    throw new TimeoutException();
                }

                Time.Sleep(TimeSpan.FromSeconds(2));
            }
        }

        private MetricsSet? GetMostRecent(string metricName)
        {
            var result = query.GetMostRecent(metricName, node);
            if (result == null) return null;
            return result.Sets.LastOrDefault();
        }
    }
}
