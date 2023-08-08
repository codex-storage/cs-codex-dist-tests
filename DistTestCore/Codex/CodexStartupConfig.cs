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

        public string LogLevelWithTopics()
        {
            var level = LogLevel.ToString()!.ToUpperInvariant();
            if (LogTopics != null && LogTopics.Count() > 0)
            {
                level = $"INFO;{level}: {string.Join(",", LogTopics.Where(s => !string.IsNullOrEmpty(s)))}";
            }
            return level;
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
