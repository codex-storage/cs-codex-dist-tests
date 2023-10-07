﻿using KubernetesWorkflow;
using Utils;

namespace CodexPlugin
{
    public class CodexStartupConfig
    {
        public string? NameOverride { get; set; }
        public ILocation Location { get; set; } = KnownLocations.UnspecifiedLocation;
        public CodexLogLevel LogLevel { get; set; }
        public CodexLogCustomTopics? CustomTopics { get; set; }
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
                    "lpstream"
                };

                level = $"{level};" +
                    $"{CustomTopics.DiscV5.ToString()!.ToLowerInvariant()}:{string.Join(",", discV5Topics)};" +
                    $"{CustomTopics.Libp2p.ToString()!.ToLowerInvariant()}:{string.Join(",", libp2pTopics)}";
            }
            return level;
        }
    }
}
