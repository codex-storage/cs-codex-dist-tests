using DistTestCore.Codex;
using DistTestCore.Marketplace;
using KubernetesWorkflow;

namespace DistTestCore
{
    public interface ICodexSetup
    {
        ICodexSetup At(Location location);
        ICodexSetup WithLogLevel(CodexLogLevel level);
        //ICodexStartupConfig WithBootstrapNode(IOnlineCodexNode node);
        ICodexSetup WithStorageQuota(ByteSize storageQuota);
        ICodexSetup EnableMetrics();
        ICodexSetup EnableMarketplace(TestToken initialBalance);
        ICodexSetup EnableMarketplace(TestToken initialBalance, Ether initialEther);
        ICodexNodeGroup BringOnline();
    }
    
    public class CodexSetup : CodexStartupConfig, ICodexSetup
    {
        private readonly CodexStarter starter;

        public int NumberOfNodes { get; }

        public CodexSetup(CodexStarter starter, int numberOfNodes)
        {
            this.starter = starter;
            NumberOfNodes = numberOfNodes;
        }

        public ICodexNodeGroup BringOnline()
        {
            return starter.BringOnline(this);
        }

        public ICodexSetup At(Location location)
        {
            Location = location;
            return this;
        }

        //public ICodexSetupConfig WithBootstrapNode(IOnlineCodexNode node)
        //{
        //    BootstrapNode = node;
        //    return this;
        //}

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
            return $"({NumberOfNodes} CodexNodes with [{args}])";
        }

        private IEnumerable<string> DescribeArgs()
        {
            if (LogLevel != null) yield return $"LogLevel={LogLevel}";
            //if (BootstrapNode != null) yield return "BootstrapNode=set-not-shown-here";
            if (StorageQuota != null) yield return $"StorageQuote={StorageQuota}";
        }
    }
}
