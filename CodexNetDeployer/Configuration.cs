using ArgsUniform;
using DistTestCore;
using DistTestCore.Codex;
using DistTestCore.Marketplace;

namespace CodexNetDeployer
{
    public class Configuration
    {
        [Uniform("codex-image", "ci", "CODEXIMAGE", true, "Docker image of Codex.")]
        public string CodexImage { get; set; } = string.Empty;

        [Uniform("geth-image", "gi", "GETHIMAGE", true, "Docker image of Geth.")]
        public string GethImage { get; set; } = string.Empty;

        [Uniform("contracts-image", "oi", "CONTRACTSIMAGE", true, "Docker image of Codex Contracts.")]
        public string ContractsImage { get; set; } = string.Empty;

        [Uniform("kube-config", "kc", "KUBECONFIG", true, "Path to Kubeconfig file.")]
        public string KubeConfigFile { get; set; } = string.Empty;

        [Uniform("kube-namespace", "kn", "KUBENAMESPACE", true, "Kubernetes namespace to be used for deployment.")]
        public string KubeNamespace { get; set; } = string.Empty;

        [Uniform("nodes", "n", "NODES", true, "Number of Codex nodes to be created.")]
        public int? NumberOfCodexNodes { get; set; }

        [Uniform("validators", "v", "VALIDATORS", true, "Number of Codex nodes that will be validating.")]
        public int? NumberOfValidators { get; set; }

        [Uniform("storage-quota", "s", "STORAGEQUOTA", true, "Storage quota in megabytes used by each Codex node.")]
        public int? StorageQuota { get; set; }

        [Uniform("log-level", "l", "LOGLEVEL", true, "Log level used by each Codex node. [Trace, Debug*, Info, Warn, Error]")]
        public CodexLogLevel CodexLogLevel { get; set; }

        public TestRunnerLocation RunnerLocation { get; set; } = TestRunnerLocation.InternalToCluster;

        public class Defaults
        {
            public string CodexImage { get; set; } = CodexContainerRecipe.DockerImage;
            public string GethImage { get; set; } = GethContainerRecipe.DockerImage;
            public string ContractsImage { get; set; } = CodexContractsContainerRecipe.DockerImage;
            public CodexLogLevel CodexLogLevel { get; set; } = CodexLogLevel.Debug;
        }

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

            return errors;
        }

        private void ForEachProperty(Action<string, string> onString, Action<string, int?> onInt)
        {
            var properties = GetType().GetProperties();
            foreach (var p in properties)
            {
                if (p.PropertyType == typeof(string)) onString(p.Name, (string)p.GetValue(this)!);
                if (p.PropertyType == typeof(int?)) onInt(p.Name, (int?)p.GetValue(this)!);
            }
        }

        private static void IntIsOverZero(string variable, int? value, List<string> errors)
        {
            if (value == null || value.Value < 1)
            {
                errors.Add($"{variable} is must be set and must be greater than 0.");
            }
        }

        private static void StringIsSet(string variable, string value, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"{variable} is must be set.");
            }
        }
    }
}
