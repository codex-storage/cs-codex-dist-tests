using NUnit.Framework;

namespace DistTestCore.Logs
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
