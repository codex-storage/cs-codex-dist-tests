using NUnit.Framework;

namespace DistTestCore
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class DontDownloadLogsAttribute : PropertyAttribute
    {
        public const string DontDownloadKey = "DontDownloadLogs";

        public DontDownloadLogsAttribute()
            : base(DontDownloadKey)
        {
        }
    }
}
