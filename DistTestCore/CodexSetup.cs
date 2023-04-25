using DistTestCore.Codex;
using DistTestCore.Marketplace;
using KubernetesWorkflow;

namespace DistTestCore
{
    public interface ICodexSetup
    {
        ICodexSetup At(Location location);
        ICodexSetup WithLogLevel(CodexLogLevel level);
        ICodexSetup WithBootstrapNode(IOnlineCodexNode node);
        ICodexSetup WithStorageQuota(ByteSize storageQuota);
        ICodexSetup EnableMetrics();
        ICodexSetup EnableMarketplace(TestToken initialBalance);
        ICodexSetup EnableMarketplace(TestToken initialBalance, Ether initialEther);
    }
    
    public class CodexSetup : CodexStartupConfig, ICodexSetup
    {
        public int NumberOfNodes { get; }

        public CodexSetup(int numberOfNodes)
        {
            NumberOfNodes = numberOfNodes;
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
            LogLevel = level;
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

        public string Describe()
        {
            var args = string.Join(',', DescribeArgs());
            return $"({NumberOfNodes} CodexNodes with args:[{args}])";
        }

        private IEnumerable<string> DescribeArgs()
        {
            if (LogLevel != null) yield return $"LogLevel={LogLevel}";
            if (BootstrapSpr != null) yield return $"BootstrapNode={BootstrapSpr}";
            if (StorageQuota != null) yield return $"StorageQuote={StorageQuota}";
        }
    }
}
