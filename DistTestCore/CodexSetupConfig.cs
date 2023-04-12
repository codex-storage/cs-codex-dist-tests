using DistTestCore.Codex;
using KubernetesWorkflow;

namespace DistTestCore
{
    public interface ICodexSetupConfig
    {
        ICodexSetupConfig At(Location location);
        ICodexSetupConfig WithLogLevel(CodexLogLevel level);
        //ICodexStartupConfig WithBootstrapNode(IOnlineCodexNode node);
        ICodexSetupConfig WithStorageQuota(ByteSize storageQuota);
        ICodexSetupConfig EnableMetrics();
        //ICodexSetupConfig EnableMarketplace(int initialBalance);
        ICodexNodeGroup BringOnline();
    }
    
    public class CodexSetupConfig : CodexStartupConfig, ICodexSetupConfig
    {
        private readonly CodexStarter starter;

        public int NumberOfNodes { get; }

        public CodexSetupConfig(CodexStarter starter, int numberOfNodes)
        {
            this.starter = starter;
            NumberOfNodes = numberOfNodes;
        }

        public ICodexNodeGroup BringOnline()
        {
            return starter.BringOnline(this);
        }

        public ICodexSetupConfig At(Location location)
        {
            Location = location;
            return this;
        }

        //public ICodexSetupConfig WithBootstrapNode(IOnlineCodexNode node)
        //{
        //    BootstrapNode = node;
        //    return this;
        //}

        public ICodexSetupConfig WithLogLevel(CodexLogLevel level)
        {
            LogLevel = level;
            return this;
        }

        public ICodexSetupConfig WithStorageQuota(ByteSize storageQuota)
        {
            StorageQuota = storageQuota;
            return this;
        }

        public ICodexSetupConfig EnableMetrics()
        {
            MetricsEnabled = true;
            return this;
        }

        //public ICodexSetupConfig EnableMarketplace(int initialBalance)
        //{
        //    MarketplaceConfig = new MarketplaceInitialConfig(initialBalance);
        //    return this;
        //}

        public string Describe()
        {
            var args = string.Join(',', DescribeArgs());
            return $"{NumberOfNodes} CodexNodes with [{args}]";
        }

        private IEnumerable<string> DescribeArgs()
        {
            if (LogLevel != null) yield return $"LogLevel={LogLevel}";
            //if (BootstrapNode != null) yield return "BootstrapNode=set-not-shown-here";
            if (StorageQuota != null) yield return $"StorageQuote={StorageQuota.SizeInBytes}";
        }
    }
}
