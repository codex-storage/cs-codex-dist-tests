using ArgsUniform;
using CodexPlugin;

namespace CodexNetDeployer
{
    public class Configuration
    {
        public const int SecondsIn1Day = 24 * 60 * 60;
        public const int TenMinutes = 10 * 60;

        [Uniform("kube-config", "kc", "KUBECONFIG", false, "Path to Kubeconfig file. Use 'null' (default) to use local cluster.")]
        public string KubeConfigFile { get; set; } = "null";

        [Uniform("kube-namespace", "kn", "KUBENAMESPACE", true, "Kubernetes namespace to be used for deployment.")]
        public string KubeNamespace { get; set; } = string.Empty;

        [Uniform("deploy-file", "df", "DEPLOYFILE", false, "Output deployment JSON file that will be written. Defaults to 'codex-deployment.json'.")]
        public string DeployFile { get; set; } = "codex-deployment.json";

        [Uniform("codex-local-repo", "cr", "CODEXLOCALREPOPATH", false, "If set, instead of using the default Codex docker image, the local repository will be used to build an image. " +
            "This requires the 'DOCKERUSERNAME' and 'DOCKERPASSWORD' environment variables to be set. (You can omit the password to use your system default, or use a docker access token as DOCKERPASSWORD.) You can set " +
            "'DOCKERTAG' to define the image tag. If not set, one will be generated.")]
        public string CodexLocalRepoPath { get; set; } = string.Empty;

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
        
        [Uniform("log-level-libp2p", "lp2p", "LOGLEVELLIBP2P", true, "Log level for all libp2p topics. [Trace, Debug, Info, Warn*, Error]")]
        public CodexLogLevel Libp2pLogLevel { get; set; } = CodexLogLevel.Warn;

        [Uniform("log-level-discv5", "ldv5", "LOGLEVELDISCV5", true, "Log level for all discv5 topics. [Trace, Debug, Info, Warn*, Error]")]
        public CodexLogLevel Discv5LogLevel { get; set; } = CodexLogLevel.Warn;

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

        [Uniform("block-mi", "bmi", "BLOCKMI", false, "Block maintenance interval in seconds. Default is 10 minutes.")]
        public int BlockMI { get; set; } = TenMinutes;

        [Uniform("block-mn", "bmn", "BLOCKMN", false, "Number of blocks maintained per interval. Default is 1000 blocks.")]
        public int BlockMN { get; set; } = 1000;

        [Uniform("metrics", "m", "METRICS", false, "[true, false]. Determines if metrics will be recorded. Default is false.")]
        public bool Metrics { get; set; } = false;

        [Uniform("teststype-podlabel", "ttpl", "TESTSTYPE-PODLABEL", false, "Each kubernetes pod will be created with a label 'teststype' with value 'continuous'. " +
            "set this option to override the label value.")]
        public string TestsTypePodLabel { get; set; } = "continuous-tests";

        [Uniform("check-connect", "cc", "CHECKCONNECT", false, "If true, deployer check ensure peer-connectivity between all deployed nodes after deployment. Default is false.")]
        public bool CheckPeerConnection { get; set; } = false;

        public List<string> Validate()
        {
            var errors = new List<string>();

            StringIsSet(nameof(KubeNamespace), KubeNamespace, errors);
            StringIsSet(nameof(KubeConfigFile), KubeConfigFile, errors);
            StringIsSet(nameof(TestsTypePodLabel), TestsTypePodLabel, errors);

            ForEachProperty(
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

        private void ForEachProperty(Action<string, int?> onInt)
        {
            var properties = GetType().GetProperties();
            foreach (var p in properties)
            {
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
