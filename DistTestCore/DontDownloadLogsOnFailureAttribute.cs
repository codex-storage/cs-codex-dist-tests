using NUnit.Framework;

namespace DistTestCore
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class DontDownloadLogsOnFailureAttribute : PropertyAttribute
    {
        public const string DontDownloadKey = "DontDownloadLogs";

        public DontDownloadLogsOnFailureAttribute()
            : base(DontDownloadKey)
        {
        }
    }
}
