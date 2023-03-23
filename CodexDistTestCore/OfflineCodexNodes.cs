namespace CodexDistTestCore
{
    public interface IOfflineCodexNodes
    {
        IOfflineCodexNodes At(CodexNodeLocation location);
        IOfflineCodexNodes WithLogLevel(CodexLogLevel level);
        IOfflineCodexNodes WithBootstrapNode(IOnlineCodexNode node);
        IOfflineCodexNodes WithStorageQuota(ByteSize storageQuota);
        ICodexNodeGroup BringOnline();
    }

    public enum CodexLogLevel
    {
        Trace,
        Debug,
        Info,
        Warn,
        Error
    }

    public enum CodexNodeLocation
    {
        Unspecified,
        BensLaptop,
        BensOldGamingMachine,
    }

    public class OfflineCodexNodes : IOfflineCodexNodes
    {
        private readonly IK8sManager k8SManager;
        
        public int NumberOfNodes { get; }
        public CodexNodeLocation Location { get; private set; }
        public CodexLogLevel? LogLevel { get; private set; }
        public IOnlineCodexNode? BootstrapNode { get; private set; }
        public ByteSize? StorageQuota { get; private set; }

        public OfflineCodexNodes(IK8sManager k8SManager, int numberOfNodes)
        {
            this.k8SManager = k8SManager;
            NumberOfNodes = numberOfNodes;
            Location = CodexNodeLocation.Unspecified;
        }

        public ICodexNodeGroup BringOnline()
        {
            return k8SManager.BringOnline(this);
        }

        public IOfflineCodexNodes At(CodexNodeLocation location)
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

        public string Describe()
        {
            var args = string.Join(',', DescribeArgs());
            return $"{NumberOfNodes} CodexNodes with [{args}]";
        }

        private IEnumerable<string> DescribeArgs()
        {
            if (LogLevel != null) yield return ($"LogLevel={LogLevel}");
            if (BootstrapNode != null) yield return ("BootstrapNode=set");
            if (StorageQuota != null) yield return ($"StorageQuote={StorageQuota.SizeInBytes}");
        }
    }
}
