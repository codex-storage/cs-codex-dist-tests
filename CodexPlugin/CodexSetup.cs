using CodexContractsPlugin;
using GethPlugin;
using KubernetesWorkflow;
using Utils;

namespace CodexPlugin
{
    public interface ICodexSetup
    {
        ICodexSetup WithLogLevel(CodexLogLevel logLevel);
        ICodexSetup WithName(string name);
        ICodexSetup At(Location location);
        ICodexSetup WithBootstrapNode(ICodexNode node);
        ICodexSetup WithStorageQuota(ByteSize storageQuota);
        ICodexSetup WithBlockTTL(TimeSpan duration);
        ICodexSetup WithBlockMaintenanceInterval(TimeSpan duration);
        ICodexSetup WithBlockMaintenanceNumber(int numberOfBlocks);
        ICodexSetup EnableMetrics();
        ICodexSetup EnableMarketplace(IGethNode gethNode, ICodexContracts codexContracts, bool isValidator = false);

        //ICodexSetup EnableMarketplace(TestToken initialBalance);
        //ICodexSetup EnableMarketplace(TestToken initialBalance, Ether initialEther);
        //ICodexSetup EnableMarketplace(TestToken initialBalance, Ether initialEther, bool isValidator);
    }
    
    public class CodexSetup : CodexStartupConfig, ICodexSetup
    {
        public int NumberOfNodes { get; }

        public CodexSetup(int numberOfNodes)
        {
            NumberOfNodes = numberOfNodes;
        }

        public ICodexSetup WithLogLevel(CodexLogLevel logLevel)
        {
            LogLevel = logLevel;
            return this;
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

        public ICodexSetup WithBootstrapNode(ICodexNode node)
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
            MetricsEnabled = true;
            return this;
        }

        public ICodexSetup EnableMarketplace(IGethNode gethNode, ICodexContracts codexContracts, bool isValidator = false)
        {
            MarketplaceConfig = new MarketplaceInitialConfig(gethNode, codexContracts, isValidator);
            return this;
        }

        //public ICodexSetup EnableMarketplace(TestToken initialBalance)
        //{
        //    return EnableMarketplace(initialBalance, 1000.Eth());
        //}

        //public ICodexSetup EnableMarketplace(TestToken initialBalance, Ether initialEther)
        //{
        //    return EnableMarketplace(initialBalance, initialEther, false);
        //}

        //public ICodexSetup EnableMarketplace(TestToken initialBalance, Ether initialEther, bool isValidator)
        //{
        //    MarketplaceConfig = new MarketplaceInitialConfig(initialEther, initialBalance, isValidator);
        //    return this;
        //}

        public string Describe()
        {
            var args = string.Join(',', DescribeArgs());
            return $"({NumberOfNodes} CodexNodes with args:[{args}])";
        }

        private IEnumerable<string> DescribeArgs()
        {
            yield return $"LogLevel={LogLevel}";
            if (BootstrapSpr != null) yield return $"BootstrapNode={BootstrapSpr}";
            if (StorageQuota != null) yield return $"StorageQuote={StorageQuota}";
        }
    }
}
