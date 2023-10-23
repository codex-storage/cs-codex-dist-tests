using KubernetesWorkflow;
using Utils;

namespace CodexPlugin
{
    public class CodexStartupConfig
    {
        public string? NameOverride { get; set; }
        public ILocation Location { get; set; } = KnownLocations.UnspecifiedLocation;
        public CodexLogLevel LogLevel { get; set; }
        public CodexLogCustomTopics? CustomTopics { get; set; } = new CodexLogCustomTopics(CodexLogLevel.Warn, CodexLogLevel.Warn);
        public ByteSize? StorageQuota { get; set; }
        public bool MetricsEnabled { get; set; }
        public MarketplaceInitialConfig? MarketplaceConfig { get; set; }
        public string? BootstrapSpr { get; set; }
        public int? BlockTTL { get; set; }
        public uint? SimulateProofFailures { get; set; }
        public bool? EnableValidator { get; set; }
        public TimeSpan? BlockMaintenanceInterval { get; set; }
        public int? BlockMaintenanceNumber { get; set; }
        public CodexTestNetConfig? PublicTestNet { get; set; }

        public string LogLevelWithTopics()
        {
            var level = LogLevel.ToString()!.ToUpperInvariant();
            if (CustomTopics != null)
            {
                var discV5Topics = new[]
                {
                    "discv5",
                    "providers",
                    "manager",
                    "cache",
                };
                var libp2pTopics = new[]
                {
                    "libp2p",
                    "multistream",
                    "switch",
                    "transport",
                    "tcptransport",
                    "semaphore",
                    "asyncstreamwrapper",
                    "lpstream",
                    "mplex",
                    "mplexchannel",
                    "noise",
                    "bufferstream",
                    "mplexcoder",
                    "secure",
                    "chronosstream",
                    "connection",
                    "connmanager",
                    "websock",
                    "ws-session"
                };

                level = $"{level};" +
                    $"{CustomTopics.DiscV5.ToString()!.ToLowerInvariant()}:{string.Join(",", discV5Topics)};" +
                    $"{CustomTopics.Libp2p.ToString()!.ToLowerInvariant()}:{string.Join(",", libp2pTopics)}";
            }
            return level;
        }
    }

    public class CodexTestNetConfig
    {
        public string PublicNatIP { get; set; } = string.Empty;
        public int PublicDiscoveryPort { get; set; }
        public int PublicListenPort { get; set; }
    }
}
