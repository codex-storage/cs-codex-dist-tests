using DistTestCore.Marketplace;
using KubernetesWorkflow;

namespace DistTestCore.Codex
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
        public bool MetricsEnabled { get; set; }
        public MarketplaceInitialConfig? MarketplaceConfig { get; set; }
        public string? BootstrapSpr { get; set; }
    }
}
