using CodexDistTestCore.Marketplace;

namespace CodexDistTestCore
{
    public interface IOfflineCodexNodes
    {
        IOfflineCodexNodes At(Location location);
        IOfflineCodexNodes WithLogLevel(CodexLogLevel level);
        IOfflineCodexNodes WithBootstrapNode(IOnlineCodexNode node);
        IOfflineCodexNodes WithStorageQuota(ByteSize storageQuota);
        IOfflineCodexNodes EnableMetrics();
        IOfflineCodexNodes EnableMarketplace(int initialBalance);
        ICodexNodeGroup BringOnline();
    }

    public enum Location
    {
        Unspecified,
        BensLaptop,
        BensOldGamingMachine,
    }

    public class OfflineCodexNodes : IOfflineCodexNodes
    {
        private readonly IK8sManager k8SManager;
        
        public int NumberOfNodes { get; }
        public Location Location { get; private set; }
        public CodexLogLevel? LogLevel { get; private set; }
        public IOnlineCodexNode? BootstrapNode { get; private set; }
        public ByteSize? StorageQuota { get; private set; }
        public bool MetricsEnabled { get; private set; }
        public MarketplaceInitialConfig? MarketplaceConfig { get; private set; }

        public OfflineCodexNodes(IK8sManager k8SManager, int numberOfNodes)
        {
            this.k8SManager = k8SManager;
            NumberOfNodes = numberOfNodes;
            Location = Location.Unspecified;
            MetricsEnabled = false;
        }

        public ICodexNodeGroup BringOnline()
        {
            return k8SManager.BringOnline(this);
        }

        public IOfflineCodexNodes At(Location location)
        {
            Location = location;
            return this;
        }

        public IOfflineCodexNodes WithBootstrapNode(IOnlineCodexNode node)
        {
            BootstrapNode = node;
            return this;
        }

        public IOfflineCodexNodes WithLogLevel(CodexLogLevel level)
        {
            LogLevel = level;
            return this;
        }

        public IOfflineCodexNodes WithStorageQuota(ByteSize storageQuota)
        {
            StorageQuota = storageQuota;
            return this;
        }

        public IOfflineCodexNodes EnableMetrics()
        {
            MetricsEnabled = true;
            return this;
        }

        public IOfflineCodexNodes EnableMarketplace(int initialBalance)
        {
            MarketplaceConfig = new MarketplaceInitialConfig(initialBalance);
            return this;
        }

        public string Describe()
        {
            var args = string.Join(',', DescribeArgs());
            return $"{NumberOfNodes} CodexNodes with [{args}]";
        }

        private IEnumerable<string> DescribeArgs()
        {
            if (LogLevel != null) yield return ($"LogLevel={LogLevel}");
            if (BootstrapNode != null) yield return ("BootstrapNode=set-not-shown-here");
            if (StorageQuota != null) yield return ($"StorageQuote={StorageQuota.SizeInBytes}");
        }
    }
}
