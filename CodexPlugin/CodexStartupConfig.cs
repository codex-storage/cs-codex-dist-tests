using KubernetesWorkflow;
using Utils;

namespace CodexPlugin
{
    public class CodexStartupConfig
    {
        public CodexStartupConfig(CodexLogLevel logLevel)
        {
            LogLevel = logLevel;
        }

        public string? NameOverride { get; set; }
        public Location Location { get; set; }
        public CodexLogLevel LogLevel { get; }
        public ByteSize? StorageQuota { get; set; }
        //public MetricsMode MetricsMode { get; set; }
        //public MarketplaceInitialConfig? MarketplaceConfig { get; set; }
        public string? BootstrapSpr { get; set; }
        public int? BlockTTL { get; set; }
        public TimeSpan? BlockMaintenanceInterval { get; set; }
        public int? BlockMaintenanceNumber { get; set; }
    }
}
