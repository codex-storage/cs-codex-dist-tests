using ArgsUniform;
using DistTestCore;
using DistTestCore.Codex;

namespace CodexNetDeployer
{
    public class Configuration
    {
        public const int SecondsIn1Day = 24 * 60 * 60;

        [Uniform("kube-config", "kc", "KUBECONFIG", false, "Path to Kubeconfig file. Use 'null' (default) to use local cluster.")]
        public string KubeConfigFile { get; set; } = "null";

        [Uniform("kube-namespace", "kn", "KUBENAMESPACE", true, "Kubernetes namespace to be used for deployment.")]
        public string KubeNamespace { get; set; } = string.Empty;

        [Uniform("nodes", "n", "NODES", true, "Number of Codex nodes to be created.")]
        public int? NumberOfCodexNodes { get; set; }

        [Uniform("validators", "v", "VALIDATORS", true, "Number of Codex nodes that will be validating.")]
        public int? NumberOfValidators { get; set; }

        [Uniform("storage-quota", "sq", "STORAGEQUOTA", true, "Storage quota in megabytes used by each Codex node.")]
        public int? StorageQuota { get; set; }

        [Uniform("storage-sell", "ss", "STORAGESELL", true, "Number of megabytes of storage quota to make available for selling.")]
        public int? StorageSell { get; set; }

        [Uniform("log-level", "l", "LOGLEVEL", true, "Log level used by each Codex node. [Trace, Debug*, Info, Warn, Error]")]
        public CodexLogLevel CodexLogLevel { get; set; } = CodexLogLevel.Debug;

        [Uniform("test-tokens", "tt", "TESTTOKENS", true, "Initial amount of test-tokens minted for each Codex node.")]
        public int InitialTestTokens { get; set; } = int.MaxValue;

        [Uniform("min-price", "mp", "MINPRICE", true, "Minimum price for the storage space for which contracts will be accepted.")]
        public int MinPrice { get; set; }

        [Uniform("max-collateral", "mc", "MAXCOLLATERAL", true, "Maximum collateral that will be placed for the total storage space.")]
        public int MaxCollateral { get; set; }

        [Uniform("max-duration", "md", "MAXDURATION", true, "Maximum duration in seconds for contracts which will be accepted.")]
        public int MaxDuration { get; set; }

        [Uniform("block-ttl", "bt", "BLOCKTTL", false, "Block timeout in seconds. Default is 24 hours.")]
        public int BlockTTL { get; set; } = SecondsIn1Day;

        [Uniform("record-metrics", "rm", "RECORDMETRICS", false, "If true, metrics will be collected for all Codex nodes.")]
        public bool RecordMetrics { get; set; } = false;

        [Uniform("teststype-podlabel", "ttpl", "TESTSTYPE-PODLABEL", false, "Each kubernetes pod will be created with a label 'teststype' with value 'continuous'. " +
            "set this option to override the label value.")]
        public string TestsTypePodLabel { get; set; } = "continuous";
       
        public TestRunnerLocation RunnerLocation { get; set; } = TestRunnerLocation.InternalToCluster;

        public List<string> Validate()
        {
            var errors = new List<string>();

            ForEachProperty(
                onString: (n, v) => StringIsSet(n, v, errors),
                onInt: (n, v) => IntIsOverZero(n, v, errors));

            if (NumberOfValidators > NumberOfCodexNodes)
            {
                errors.Add($"{nameof(NumberOfValidators)} ({NumberOfValidators}) may not be greater than {nameof(NumberOfCodexNodes)} ({NumberOfCodexNodes}).");
            }
            if (StorageSell.HasValue && StorageQuota.HasValue && StorageSell.Value >= StorageQuota.Value)
            {
                errors.Add("StorageSell cannot be greater than or equal to StorageQuota.");
            }

            return errors;
        }

        private void ForEachProperty(Action<string, string> onString, Action<string, int?> onInt)
        {
            var properties = GetType().GetProperties();
            foreach (var p in properties)
            {
                if (p.PropertyType == typeof(string)) onString(p.Name, (string)p.GetValue(this)!);
                if (p.PropertyType == typeof(int?)) onInt(p.Name, (int?)p.GetValue(this)!);
                if (p.PropertyType == typeof(int)) onInt(p.Name, (int)p.GetValue(this)!);
            }
        }

        private static void IntIsOverZero(string variable, int? value, List<string> errors)
        {
            if (value == null || value.Value < 1)
            {
                errors.Add($"{variable} must be set and must be greater than 0.");
            }
        }

        private static void StringIsSet(string variable, string value, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"{variable} must be set.");
            }
        }
    }
}
