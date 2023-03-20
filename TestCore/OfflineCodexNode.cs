namespace CodexDistTests.TestCore
{
    public interface IOfflineCodexNode
    {
        IOfflineCodexNode WithLogLevel(CodexLogLevel level);
        IOfflineCodexNode WithBootstrapNode(IOnlineCodexNode node);
        IOfflineCodexNode WithStorageQuota(int storageQuotaBytes);
        IOnlineCodexNode BringOnline();
    }

    public enum CodexLogLevel
    {
        Trace,
        Debug,
        Info,
        Warn,
        Error
    }

    public class OfflineCodexNode : IOfflineCodexNode
    {
        private readonly IK8sManager k8SManager;

        public CodexLogLevel? LogLevel { get; private set; }
        public IOnlineCodexNode? BootstrapNode { get; private set; }
        public int? StorageQuota { get; private set; }

        public OfflineCodexNode(IK8sManager k8SManager)
        {
            this.k8SManager = k8SManager;
        }

        public IOnlineCodexNode BringOnline()
        {
            return k8SManager.BringOnline(this);
        }

        public IOfflineCodexNode WithBootstrapNode(IOnlineCodexNode node)
        {
            BootstrapNode = node;
            return this;
        }

        public IOfflineCodexNode WithLogLevel(CodexLogLevel level)
        {
            LogLevel = level;
            return this;
        }

        public IOfflineCodexNode WithStorageQuota(int storageQuotaBytes)
        {
            StorageQuota = storageQuotaBytes;
            return this;
        }

        public string Describe()
        {
            var result = "";
            if (LogLevel != null) result += $"LogLevel={LogLevel},";
            if (BootstrapNode != null) result += "BootstrapNode=set,";
            if (StorageQuota != null) result += $"StorageQuote={StorageQuota},";
            return result;
        }
    }
}
