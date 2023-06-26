using ArgsUniform;
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

        public CodexDeployment CodexDeployment { get; set; } = null!;
    }

    public class ConfigLoader
    {
        public Configuration Load(string[] args)
        {
            var uniformArgs = new ArgsUniform<Configuration>(args);

            var result = uniformArgs.Parse();
            
            result.CodexDeployment = ParseCodexDeploymentJson(result.CodexDeploymentJson);

            return result;
        }
        
        private CodexDeployment ParseCodexDeploymentJson(string filename)
        {
            var d = JsonConvert.DeserializeObject<CodexDeployment>(File.ReadAllText(filename))!;
            if (d == null) throw new Exception("Unable to parse " + filename);
            return d;
        }
    }
}
