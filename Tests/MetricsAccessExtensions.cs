using DistTestCore.Helpers;
using Logging;
using MetricsPlugin;
using NUnit.Framework.Constraints;

namespace Tests
{
    public static class MetricsAccessExtensions
    {
        public static void AssertThat(this IMetricsAccess access, string metricName, IResolveConstraint constraint, ILog? log = null, string message = "")
        {
            AssertHelpers.RetryAssert(constraint, () =>
            {
                var metricSet = access.GetMetric(metricName);
                var metricValue = metricSet.Values[0].Value;

                if (log != null) log.Log($"{access.TargetName} metric '{metricName}' = {metricValue}");
                return metricValue;
            }, message);
        }
    }
}
