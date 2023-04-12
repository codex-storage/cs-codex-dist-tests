using KubernetesWorkflow;

namespace DistTestCore.Codex
{
    public class CodexStartupConfig
    {
        public Location Location { get; set; }
        public CodexLogLevel? LogLevel { get; set; }
        public ByteSize? StorageQuota { get; set; }
        public bool MetricsEnabled { get; set; }
    }
}
