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
        private readonly MetricsQuery query;
        private readonly OnlineCodexNode[] nodes;

        public MetricsAccess(MetricsQuery query, OnlineCodexNode[] nodes)
        {
            this.query = query;
            this.nodes = nodes;
        }

        public void AssertThat(IOnlineCodexNode node, string metricName, IResolveConstraint constraint, string message = "")
        {
            var n = (OnlineCodexNode)node;
            CollectionAssert.Contains(nodes, n, "Incorrect test setup: Attempt to get metrics for OnlineCodexNode from the wrong MetricsAccess object. " +
                "(This CodexNode is tracked by a different instance.)");

            var metricSet = GetMetricWithTimeout(metricName, n);
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
            var result = query.GetMostRecent(metricName);
            if (result == null) return null;

            var pod = node.Group.PodInfo!;
            var instance = $"{pod.Ip}:{node.Container.MetricsPort}";
            return result.Sets.SingleOrDefault(r => r.Instance == instance);
        }
    }
}
