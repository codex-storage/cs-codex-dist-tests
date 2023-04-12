namespace DistTestCore.Codex
{
    public class CodexStartupConfig
    {
        public Location Location { get; set; }
        public CodexLogLevel? LogLevel { get; set; }
        public ByteSize? StorageQuota { get; set; }
        public bool MetricsEnabled { get; set; }

        //public IOnlineCodexNode? BootstrapNode { get; private set; }
        //public MarketplaceInitialConfig? MarketplaceConfig { get; private set; }
    }
}
