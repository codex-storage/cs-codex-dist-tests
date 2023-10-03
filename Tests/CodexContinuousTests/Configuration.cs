using ArgsUniform;
using CodexPlugin;
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

        [Uniform("stop", "s", "STOPONFAIL", false, "If greater than zero, runner will stop after this many test failures and download all cluster container logs. 0 by default.")]
        public int StopOnFailure { get; set; } = 0;

        [Uniform("target-duration", "td", "TARGETDURATION", false, "If greater than zero, runner will run for this many seconds before stopping.")]
        public int TargetDurationSeconds { get; set; } = 0;

        [Uniform("filter", "f", "FILTER", false, "If set, runs only tests whose names contain any of the filter strings. Comma-separated. Case sensitive.")]
        public string Filter { get; set; } = string.Empty;

        [Uniform("cleanup", "cl", "CLEANUP", false, "If set, the kubernetes namespace will be deleted after the test run has finished.")]
        public bool Cleanup { get; set; } = false;

        public CodexDeployment CodexDeployment { get; set; } = null!;
    }

    public class ConfigLoader
    {
        public Configuration Load(string[] args)
        {
            var uniformArgs = new ArgsUniform<Configuration>(PrintHelp, args);

            var result = uniformArgs.Parse(true);
            result.CodexDeployment = ParseCodexDeploymentJson(result.CodexDeploymentJson);
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
            Console.WriteLine("ContinuousTests will run a set of tests against a codex deployment given a codex-deployment.json file." + nl +
                "The tests will run in an endless loop unless otherwise specified." + nl);
        }
    }
}
