using CodexContractsPlugin;
using GethPlugin;
using KubernetesWorkflow;
using Utils;

namespace CodexPlugin
{
    public interface ICodexSetup
    {
        ICodexSetup WithName(string name);
        ICodexSetup At(ILocation location);
        ICodexSetup WithBootstrapNode(ICodexNode node);
        ICodexSetup WithLogLevel(CodexLogLevel level);
        ICodexSetup WithLogLevel(CodexLogLevel level, CodexLogCustomTopics customTopics);
        ICodexSetup WithStorageQuota(ByteSize storageQuota);
        ICodexSetup WithBlockTTL(TimeSpan duration);
        ICodexSetup WithBlockMaintenanceInterval(TimeSpan duration);
        ICodexSetup WithBlockMaintenanceNumber(int numberOfBlocks);
        ICodexSetup EnableMetrics();
        ICodexSetup EnableMarketplace(IGethNode gethNode, ICodexContracts codexContracts, Ether initialEth, TestToken initialTokens);
        ICodexSetup EnableMarketplace(IGethNode gethNode, ICodexContracts codexContracts, Ether initialEth, TestToken initialTokens, Action<IMarketplaceSetup> marketplaceSetup);
        /// <summary>
        /// Provides an invalid proof every N proofs
        /// </summary>
        ICodexSetup WithSimulateProofFailures(uint failEveryNProofs);
        ICodexSetup AsPublicTestNet(CodexTestNetConfig testNetConfig);
    }

    public interface IMarketplaceSetup
    {
        IMarketplaceSetup AsStorageNode();
        IMarketplaceSetup AsValidator();
    }

    public class CodexLogCustomTopics
    {
        public CodexLogCustomTopics(CodexLogLevel discV5, CodexLogLevel libp2p, CodexLogLevel blockExchange)
        {
            DiscV5 = discV5;
            Libp2p = libp2p;
            BlockExchange = blockExchange;
        }

        public CodexLogCustomTopics(CodexLogLevel discV5, CodexLogLevel libp2p)
        {
            DiscV5 = discV5;
            Libp2p = libp2p;
        }

        public CodexLogLevel DiscV5 { get; set; }
        public CodexLogLevel Libp2p { get; set; }
        public CodexLogLevel? BlockExchange { get; }
    }

    public class CodexSetup : CodexStartupConfig, ICodexSetup
    {
        public int NumberOfNodes { get; }

        public CodexSetup(int numberOfNodes)
        {
            NumberOfNodes = numberOfNodes;
        }

        public ICodexSetup WithName(string name)
        {
            NameOverride = name;
            return this;
        }

        public ICodexSetup At(ILocation location)
        {
            Location = location;
            return this;
        }

        public ICodexSetup WithBootstrapNode(ICodexNode node)
        {
            BootstrapSpr = node.GetDebugInfo().Spr;
            return this;
        }

        public ICodexSetup WithLogLevel(CodexLogLevel level)
        {
            LogLevel = level;
            return this;
        }

        public ICodexSetup WithLogLevel(CodexLogLevel level, CodexLogCustomTopics customTopics)
        {
            LogLevel = level;
            CustomTopics = customTopics;
            return this;
        }

        public ICodexSetup WithStorageQuota(ByteSize storageQuota)
        {
            StorageQuota = storageQuota;
            return this;
        }

        public ICodexSetup WithBlockTTL(TimeSpan duration)
        {
            BlockTTL = Convert.ToInt32(duration.TotalSeconds);
            return this;
        }

        public ICodexSetup WithBlockMaintenanceInterval(TimeSpan duration)
        {
            BlockMaintenanceInterval = duration;
            return this;
        }

        public ICodexSetup WithBlockMaintenanceNumber(int numberOfBlocks)
        {
            BlockMaintenanceNumber = numberOfBlocks;
            return this;
        }

        public ICodexSetup EnableMetrics()
        {
            MetricsEnabled = true;
            return this;
        }

        public ICodexSetup EnableMarketplace(IGethNode gethNode, ICodexContracts codexContracts, Ether initialEth, TestToken initialTokens)
        {
            return EnableMarketplace(gethNode, codexContracts, initialEth, initialTokens, s => { });
        }

        public ICodexSetup EnableMarketplace(IGethNode gethNode, ICodexContracts codexContracts, Ether initialEth, TestToken initialTokens, Action<IMarketplaceSetup> marketplaceSetup)
        {
            var ms = new MarketplaceSetup();
            marketplaceSetup(ms);

            MarketplaceConfig = new MarketplaceInitialConfig(ms, gethNode, codexContracts, initialEth, initialTokens);
            return this;
        }

        public ICodexSetup WithSimulateProofFailures(uint failEveryNProofs)
        {
            SimulateProofFailures = failEveryNProofs;
            return this;
        }

        public ICodexSetup AsPublicTestNet(CodexTestNetConfig testNetConfig)
        {
            PublicTestNet = testNetConfig;
            return this;
        }

        public string Describe()
        {
            var args = string.Join(',', DescribeArgs());
            return $"({NumberOfNodes} CodexNodes with args:[{args}])";
        }

        private IEnumerable<string> DescribeArgs()
        {
            if (PublicTestNet != null) yield return $"<!>Public TestNet with listenPort: {PublicTestNet.PublicListenPort}<!>";
            yield return $"LogLevel={LogLevelWithTopics()}";
            if (BootstrapSpr != null) yield return $"BootstrapNode={BootstrapSpr}";
            if (StorageQuota != null) yield return $"StorageQuota={StorageQuota}";
            if (SimulateProofFailures != null) yield return $"SimulateProofFailures={SimulateProofFailures}";
            if (MarketplaceConfig != null) yield return $"MarketplaceSetup={MarketplaceConfig.MarketplaceSetup}";
        }
    }

    public class MarketplaceSetup : IMarketplaceSetup
    {
        public bool IsStorageNode { get; private set; }
        public bool IsValidator { get; private set; }

        public IMarketplaceSetup AsStorageNode()
        {
            IsStorageNode = true;
            return this;
        }

        public IMarketplaceSetup AsValidator()
        {
            IsValidator = true;
            return this;
        }

        public override string ToString()
        {
            var result = "[(clientNode)"; // When marketplace is enabled, being a clientNode is implicit.
            result += IsStorageNode ? "(storageNode)" : "()";
            result += IsValidator ? "(validator)" : "()";
            result += "]";
            return result;
        }
    }

}
