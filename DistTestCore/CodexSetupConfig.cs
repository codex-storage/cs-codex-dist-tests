using DistTestCore.Codex;

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

    public enum Location
    {
        Unspecified,
        BensLaptop,
        BensOldGamingMachine,
    }

    public class CodexSetupConfig : ICodexSetupConfig
    {
        private readonly CodexStarter starter;

        public int NumberOfNodes { get; }
        public Location Location { get; private set; }
        public CodexLogLevel? LogLevel { get; private set; }
        //public IOnlineCodexNode? BootstrapNode { get; private set; }
        public ByteSize? StorageQuota { get; private set; }
        public bool MetricsEnabled { get; private set; }
        //public MarketplaceInitialConfig? MarketplaceConfig { get; private set; }

        public CodexSetupConfig(CodexStarter starter, int numberOfNodes)
        {
            this.starter = starter;
            NumberOfNodes = numberOfNodes;
            Location = Location.Unspecified;
            MetricsEnabled = false;
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
