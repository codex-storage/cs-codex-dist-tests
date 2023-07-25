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
        public CodexLogLevel LogLevel { get; set; }
        public string[]? LogTopics { get; set; }
        public ByteSize? StorageQuota { get; set; }
        public bool MetricsEnabled { get; set; }
        public MarketplaceInitialConfig? MarketplaceConfig { get; set; }
        public string? BootstrapSpr { get; set; }
        public int? BlockTTL { get; set; }
        public uint? SimulateProofFailures { get; set; }
        public bool? EnableValidator { get; set; }
    }
}
