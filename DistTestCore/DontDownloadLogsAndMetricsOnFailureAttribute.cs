using NUnit.Framework;

namespace DistTestCore
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class DontDownloadLogsAndMetricsOnFailureAttribute : PropertyAttribute
    {
        public const string DontDownloadKey = "DontDownloadLogsAndMetrics";

        public DontDownloadLogsAndMetricsOnFailureAttribute()
            : base(DontDownloadKey)
        {
        }
    }
}
