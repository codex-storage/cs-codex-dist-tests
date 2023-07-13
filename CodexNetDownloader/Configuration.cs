using ArgsUniform;
using DistTestCore;
using DistTestCore.Codex;

namespace CodexNetDownloader
{
    public class Configuration
    {
        [Uniform("output-path", "o", "OUTPUT", true, "Path where files will be written.")]
        public string OutputPath { get; set; } = "output";

        [Uniform("codex-deployment", "c", "CODEXDEPLOYMENT", true, "Path to codex-deployment JSON file.")]
        public string CodexDeploymentJson { get; set; } = string.Empty;

        [Uniform("kube-config", "kc", "KUBECONFIG", true, "Path to Kubeconfig file. Use 'null' (default) to use local cluster.")]
        public string KubeConfigFile { get; set; } = "null";

        public CodexDeployment CodexDeployment { get; set; } = null!;

        public TestRunnerLocation RunnerLocation { get; set; } = TestRunnerLocation.InternalToCluster;
    }
}
