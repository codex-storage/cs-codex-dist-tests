using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace CodexDistTestCore.Metrics
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
        private readonly OnlineCodexNode node;

        public MetricsAccess(MetricsQuery query, OnlineCodexNode node)
        {
            this.query = query;
            this.node = node;
        }

        public void AssertThat(string metricName, IResolveConstraint constraint, string message = "")
        {
            var metricSet = GetMetricWithTimeout(metricName, node);
            var metricValue = metricSet.Values[0].Value;
            Assert.That(metricValue, constraint, message);
        }

        private MetricsSet GetMetricWithTimeout(string metricName, OnlineCodexNode node)
        {
            var start = DateTime.UtcNow;

            while (true)
            {
                var mostRecent = GetMostRecent(metricName, node);
                if (mostRecent != null) return mostRecent;
                if (DateTime.UtcNow - start > Timing.WaitForMetricTimeout())
                {
                    Assert.Fail($"Timeout: Unable to get metric '{metricName}'.");
                    throw new TimeoutException();
                }

                Utils.Sleep(TimeSpan.FromSeconds(2));
            }
        }

        private MetricsSet? GetMostRecent(string metricName, OnlineCodexNode node)
        {
            var result = query.GetMostRecent(metricName, node);
            if (result == null) return null;
            return result.Sets.LastOrDefault();
        }
    }
}
