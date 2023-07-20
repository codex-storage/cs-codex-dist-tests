using ArgsUniform;
using DistTestCore;
using DistTestCore.Codex;
using Newtonsoft.Json;

namespace ContinuousTests
{
    public class Configuration
    {
        [Uniform("log-path", "l", "LOGPATH", true, "Path where log files will be written.")]
        public string LogPath { get; set; } = "logs";

        [Uniform("data-path", "d", "DATAPATH", true, "Path where temporary data files will be written.")]
        public string DataPath { get; set; } = "data";

        [Uniform("codex-deployment", "c", "CODEXDEPLOYMENT", true, "Path to codex-deployment JSON file.")]
        public string CodexDeploymentJson { get; set; } = string.Empty;

        [Uniform("keep", "k", "KEEP", false, "Set to '1' to retain logs of successful tests.")]
        public bool KeepPassedTestLogs { get; set; } = false;

        [Uniform("kube-config", "kc", "KUBECONFIG", true, "Path to Kubeconfig file. Use 'null' (default) to use local cluster.")]
        public string KubeConfigFile { get; set; } = "null";

        [Uniform("stop", "s", "STOPONFAIL", false, "If true, runner will stop on first test failure and download all cluster container logs. False by default.")]
        public bool StopOnFailure { get; set; } = false;

        [Uniform("dl-logs", "dl", "DLLOGS", false, "If true, runner will periodically download and save/append container logs to the log path.")]
        public bool DownloadContainerLogs { get; set; } = false;

        public CodexDeployment CodexDeployment { get; set; } = null!;

        public TestRunnerLocation RunnerLocation { get; set; } = TestRunnerLocation.InternalToCluster;
    }

    public class ConfigLoader
    {
        public Configuration Load(string[] args)
        {
            var uniformArgs = new ArgsUniform<Configuration>(PrintHelp, args);

            var result = uniformArgs.Parse(true);
            
            result.CodexDeployment = ParseCodexDeploymentJson(result.CodexDeploymentJson);
            if (args.Any(a => a == "--external"))
            {
                result.RunnerLocation = TestRunnerLocation.ExternalToCluster;
            }

            return result;
        }
        
        private CodexDeployment ParseCodexDeploymentJson(string filename)
        {
            var d = JsonConvert.DeserializeObject<CodexDeployment>(File.ReadAllText(filename))!;
            if (d == null) throw new Exception("Unable to parse " + filename);
            return d;
        }

        private static void PrintHelp()
        {
            var nl = Environment.NewLine;
            Console.WriteLine("CodexNetDownloader lets you download all container logs given a codex-deployment.json file." + nl);

            Console.WriteLine("CodexNetDownloader assumes you are running this tool from *inside* the Kubernetes cluster. " +
                "If you are not running this from a container inside the cluster, add the argument '--external'." + nl);
        }
    }
}
