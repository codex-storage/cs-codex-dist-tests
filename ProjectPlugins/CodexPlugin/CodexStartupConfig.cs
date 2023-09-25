using KubernetesWorkflow;
using Utils;

namespace CodexPlugin
{
    public class CodexStartupConfig
    {
        public string? NameOverride { get; set; }
        public ILocation Location { get; set; } = KnownLocations.UnspecifiedLocation;
        public CodexLogLevel LogLevel { get; set; }
        public string[]? LogTopics { get; set; }
        public ByteSize? StorageQuota { get; set; }
        public bool MetricsEnabled { get; set; }
        public MarketplaceInitialConfig? MarketplaceConfig { get; set; }
        public string? BootstrapSpr { get; set; }
        public int? BlockTTL { get; set; }
        public uint? SimulateProofFailures { get; set; }
        public bool? EnableValidator { get; set; }
        public TimeSpan? BlockMaintenanceInterval { get; set; }
        public int? BlockMaintenanceNumber { get; set; }

        public string LogLevelWithTopics()
        {
            var level = LogLevel.ToString()!.ToUpperInvariant();
            if (LogTopics != null && LogTopics.Count() > 0)
            {
                level = $"INFO;{level}: {string.Join(",", LogTopics.Where(s => !string.IsNullOrEmpty(s)))}";
            }
            return level;
        }
    }
}
