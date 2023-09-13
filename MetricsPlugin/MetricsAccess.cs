using Utils;

namespace MetricsPlugin
{
    public interface IMetricsAccess
    {
        Metrics? GetAllMetrics();
        MetricsSet GetMetric(string metricName);
        MetricsSet GetMetric(string metricName, TimeSpan timeout);
    }

    public class MetricsAccess : IMetricsAccess
    {
        private readonly MetricsQuery query;
        private readonly IMetricsScrapeTarget target;

        public MetricsAccess(MetricsQuery query, IMetricsScrapeTarget target)
        {
            this.query = query;
            this.target = target;
        }

        //public void AssertThat(string metricName, IResolveConstraint constraint, string message = "")
        //{
        //    AssertHelpers.RetryAssert(constraint, () =>
        //    {
        //        var metricSet = GetMetricWithTimeout(metricName);
        //        var metricValue = metricSet.Values[0].Value;

        //        log.Log($"{node.Name} metric '{metricName}' = {metricValue}");
        //        return metricValue;
        //    }, message);
        //}

        public Metrics? GetAllMetrics()
        {
            return query.GetAllMetricsForNode(target);
        }

        public MetricsSet GetMetric(string metricName)
        {
            return GetMetric(metricName, TimeSpan.FromSeconds(10));
        }

        public MetricsSet GetMetric(string metricName, TimeSpan timeout)
        {
            var start = DateTime.UtcNow;

            while (true)
            {
                var mostRecent = GetMostRecent(metricName);
                if (mostRecent != null) return mostRecent;
                if (DateTime.UtcNow - start > timeout)
                {
                    throw new TimeoutException();
                }

                Time.Sleep(TimeSpan.FromSeconds(2));
            }
        }

        private MetricsSet? GetMostRecent(string metricName)
        {
            var result = query.GetMostRecent(metricName, target);
            if (result == null) return null;
            return result.Sets.LastOrDefault();
        }
    }
}
