using DistTestCore.Codex;
using DistTestCore.Marketplace;
using KubernetesWorkflow;

namespace DistTestCore
{
    public interface ICodexSetup
    {
        ICodexSetup WithName(string name);
        ICodexSetup At(Location location);
        ICodexSetup WithLogLevel(CodexLogLevel level);
        /// <summary>
        /// Sets the log level for codex. The default level is INFO and the
        /// log level is applied only to the supplied topics.
        /// </summary>
        ICodexSetup WithLogLevel(CodexLogLevel level, IEnumerable<string>? topics);
        ICodexSetup WithBootstrapNode(IOnlineCodexNode node);
        ICodexSetup WithStorageQuota(ByteSize storageQuota);
        ICodexSetup EnableMetrics();
        ICodexSetup EnableMarketplace(TestToken initialBalance);
        ICodexSetup EnableMarketplace(TestToken initialBalance, Ether initialEther);
        /// <summary>
        /// Provides an invalid proof every N proofs
        /// </summary>
        ICodexSetup WithSimulateProofFailures(uint failEveryNProofs);
        /// <summary>
        /// Enables the validation module in the node
        /// </summary>
        ICodexSetup WithValidator();
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

        public ICodexSetup At(Location location)
        {
            Location = location;
            return this;
        }

        public ICodexSetup WithBootstrapNode(IOnlineCodexNode node)
        {
            BootstrapSpr = node.GetDebugInfo().spr;
            return this;
        }

        public ICodexSetup WithLogLevel(CodexLogLevel level)
        {
            return WithLogLevel(level, null);
        }

        public ICodexSetup WithLogLevel(CodexLogLevel level, IEnumerable<string>? topics)
        {
            LogLevel = level;
            LogTopics = topics;
            return this;
        }

        public ICodexSetup WithStorageQuota(ByteSize storageQuota)
        {
            StorageQuota = storageQuota;
            return this;
        }

        public ICodexSetup EnableMetrics()
        {
            MetricsEnabled = true;
            return this;
        }

        public ICodexSetup EnableMarketplace(TestToken initialBalance)
        {
            return EnableMarketplace(initialBalance, 1000.Eth());
        }

        public ICodexSetup EnableMarketplace(TestToken initialBalance, Ether initialEther)
        {
            MarketplaceConfig = new MarketplaceInitialConfig(initialEther, initialBalance);
            return this;
        }

        public ICodexSetup WithSimulateProofFailures(uint failEveryNProofs)
        {
            SimulateProofFailures = failEveryNProofs;
            return this;
        }

        public ICodexSetup WithValidator()
        {
            EnableValidator = true;
            return this;
        }

        public string Describe()
        {
            var args = string.Join(',', DescribeArgs());
            return $"({NumberOfNodes} CodexNodes with args:[{args}])";
        }

        private IEnumerable<string> DescribeArgs()
        {
            if (LogLevel != null) yield return $"LogLevel={LogLevel}";
            if (BootstrapSpr != null) yield return $"BootstrapNode={BootstrapSpr}";
            if (StorageQuota != null) yield return $"StorageQuota={StorageQuota}";
            if (SimulateProofFailures != null) yield return $"SimulateProofFailures={SimulateProofFailures}";
            if (EnableValidator != null) yield return $"EnableValidator={EnableValidator}";
        }
    }
}
