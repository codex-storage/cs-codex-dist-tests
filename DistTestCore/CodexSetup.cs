using DistTestCore.Codex;
using DistTestCore.Marketplace;
using KubernetesWorkflow;
using Utils;

namespace DistTestCore
{
    public interface ICodexSetup
    {
        ICodexSetup WithName(string name);
        ICodexSetup At(Location location);
        ICodexSetup WithBootstrapNode(IOnlineCodexNode node);
        ICodexSetup WithStorageQuota(ByteSize storageQuota);
        ICodexSetup WithBlockTTL(TimeSpan duration);
        ICodexSetup WithBlockMaintenanceInterval(TimeSpan duration);
        ICodexSetup WithBlockMaintenanceNumber(int numberOfBlocks);
        ICodexSetup EnableMetrics();
        ICodexSetup EnableMarketplace(TestToken initialBalance);
        ICodexSetup EnableMarketplace(TestToken initialBalance, Ether initialEther);
        ICodexSetup EnableMarketplace(TestToken initialBalance, Ether initialEther, bool isValidator);
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

        public CodexSetup(int numberOfNodes, CodexLogLevel logLevel)
            : base(logLevel)
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
            MetricsMode = Metrics.MetricsMode.Record;
            return this;
        }

        public ICodexSetup EnableMarketplace(TestToken initialBalance)
        {
            return EnableMarketplace(initialBalance, 1000.Eth());
        }

        public ICodexSetup EnableMarketplace(TestToken initialBalance, Ether initialEther)
        {
			return EnableMarketplace(initialBalance, initialEther, false);
        }

        public ICodexSetup EnableMarketplace(TestToken initialBalance, Ether initialEther, bool isValidator)
        {
            MarketplaceConfig = new MarketplaceInitialConfig(initialEther, initialBalance, isValidator);
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
            yield return $"LogLevel={LogLevel}";
            if (BootstrapSpr != null) yield return $"BootstrapNode={BootstrapSpr}";
            if (StorageQuota != null) yield return $"StorageQuota={StorageQuota}";
            if (SimulateProofFailures != null) yield return $"SimulateProofFailures={SimulateProofFailures}";
            if (EnableValidator != null) yield return $"EnableValidator={EnableValidator}";
        }
    }
}
