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
        public string CodexDeploymentJson { get; set; } = string.Empty; // @"d:\Projects\cs-codex-dist-tests\CodexNetDownloader\codex-deployment.json";

        [Uniform("kube-config", "kc", "KUBECONFIG", true, "Path to Kubeconfig file. Use 'null' (default) to use local cluster.")]
        public string KubeConfigFile { get; set; } = string.Empty;// @"c:\Users\Ben\.kube\codex-tests-ams3-dev-kubeconfig.yaml";

        public CodexDeployment CodexDeployment { get; set; } = null!;

        public TestRunnerLocation RunnerLocation { get; set; } = TestRunnerLocation.ExternalToCluster;
    }
}
