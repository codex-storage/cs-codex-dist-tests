using CodexClient;
using KubernetesWorkflow;
using Utils;

namespace CodexPlugin
{
    public class CodexStartupConfig
    {
        public string? NameOverride { get; set; }
        public ILocation Location { get; set; } = KnownLocations.UnspecifiedLocation;
        public CodexLogLevel LogLevel { get; set; }
        public CodexLogCustomTopics? CustomTopics { get; set; } = new CodexLogCustomTopics(CodexLogLevel.Info, CodexLogLevel.Warn);
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
                    "routingtable",
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
                    // Removed: "connmanager", is used for transcript peer-dropped event.
                    "websock",
                    "ws-session",
                    // Removed: "dialer", is used for transcript successful-dial event.
                    "muxedupgrade",
                    "upgrade",
                    "identify"
                };
                var blockExchangeTopics = new[]
                {
                    "codex",
                    "pendingblocks",
                    "peerctxstore",
                    "discoveryengine",
                    "blockexcengine",
                    "blockexcnetwork",
                    "blockexcnetworkpeer"
                };
                var contractClockTopics = new[]
                {
                    "contracts",
                    "clock"
                };
                var jsonSerializeTopics = new[]
                {
                    "serde",
                    "json",
                    "serialization"
                };
                var marketplaceInfraTopics = new[]
                {
                    "JSONRPC-WS-CLIENT",
                    "JSONRPC-HTTP-CLIENT",
                };

                var alwaysIgnoreTopics = new []
                {
                    "JSONRPC-CLIENT"
                };

                level = $"{level};" +
                    $"{CustomTopics.DiscV5.ToString()!.ToLowerInvariant()}:{string.Join(",", discV5Topics)};" +
                    $"{CustomTopics.Libp2p.ToString()!.ToLowerInvariant()}:{string.Join(",", libp2pTopics)};" +
                    $"{CustomTopics.ContractClock.ToString().ToLowerInvariant()}:{string.Join(",", contractClockTopics)};" +
                    $"{CustomTopics.JsonSerialize.ToString().ToLowerInvariant()}:{string.Join(",", jsonSerializeTopics)};" +
                    $"{CustomTopics.MarketplaceInfra.ToString().ToLowerInvariant()}:{string.Join(",", marketplaceInfraTopics)};" +
                    $"{CodexLogLevel.Error.ToString()}:{string.Join(",", alwaysIgnoreTopics)}";

                if (CustomTopics.BlockExchange != null)
                {
                    level += $";{CustomTopics.BlockExchange.ToString()!.ToLowerInvariant()}:{string.Join(",", blockExchangeTopics)}";
                }
            }
            return level;
        }
    }

    public class CodexTestNetConfig
    {
        public int PublicDiscoveryPort { get; set; }
        public int PublicListenPort { get; set; }
    }
}
