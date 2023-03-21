namespace CodexDistTestCore
{
    public interface IOfflineCodexNodes
    {
        IOfflineCodexNodes WithLogLevel(CodexLogLevel level);
        IOfflineCodexNodes WithBootstrapNode(IOnlineCodexNode node);
        IOfflineCodexNodes WithStorageQuota(ByteSize storageQuota);
        IOnlineCodexNodes BringOnline();
    }

    public enum CodexLogLevel
    {
        Trace,
        Debug,
        Info,
        Warn,
        Error
    }

    public class OfflineCodexNodes : IOfflineCodexNodes
    {
        private readonly IK8sManager k8SManager;
        
        public int NumberOfNodes { get; }
        public CodexLogLevel? LogLevel { get; private set; }
        public IOnlineCodexNode? BootstrapNode { get; private set; }
        public ByteSize? StorageQuota { get; private set; }

        public OfflineCodexNodes(IK8sManager k8SManager, int numberOfNodes)
        {
            this.k8SManager = k8SManager;
            NumberOfNodes = numberOfNodes;
        }

        public IOnlineCodexNodes BringOnline()
        {
            return k8SManager.BringOnline(this);
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
            var result = "";
            if (LogLevel != null) result += $"LogLevel={LogLevel},";
            if (BootstrapNode != null) result += "BootstrapNode=set,";
            if (StorageQuota != null) result += $"StorageQuote={StorageQuota.SizeInBytes},";
            return result;
        }
    }
}
