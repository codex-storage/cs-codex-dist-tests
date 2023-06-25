using DistTestCore.Codex;
using Newtonsoft.Json;

namespace ContinuousTests
{
    public class Configuration
    {
        public string LogPath { get; set; } = string.Empty;
        public string DataPath { get; set; } = string.Empty;
        public CodexDeployment CodexDeployment { get; set; } = null!;
        public bool KeepPassedTestLogs { get; set; }
    }

    public class ConfigLoader
    {
        private const string filename = "config.json";

        public Configuration Load()
        {
            var config = Read();

            Validate(config);
            return config;
        }

        private Configuration Read()
        {
            if (File.Exists(filename))
            {
                var lines = File.ReadAllText(filename);
                try
                {
                    var result = JsonConvert.DeserializeObject<Configuration>(lines);
                    if (result != null) return result;
                }
                catch { }
            }

            var logPath = Environment.GetEnvironmentVariable("LOGPATH");
            var dataPath = Environment.GetEnvironmentVariable("DATAPATH");
            var codexDeploymentJson = Environment.GetEnvironmentVariable("CODEXDEPLOYMENT");
            var keep = Environment.GetEnvironmentVariable("KEEPPASSEDTESTLOGS");

            if (!string.IsNullOrEmpty(logPath) &&
                !string.IsNullOrEmpty(dataPath) &&
                !string.IsNullOrEmpty(codexDeploymentJson))
            {
                try 
                {
                    return new Configuration
                    { 
                        LogPath = logPath,
                        DataPath = dataPath,
                        CodexDeployment = ParseCodexDeploymentJson(codexDeploymentJson),
                        KeepPassedTestLogs = keep == "1"
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: " + ex);
                }
            }

            var nl = Environment.NewLine;
            throw new Exception($"Unable to load configuration from '{filename}', and " +
                "unable to load configuration from environment variables. " + nl +
                "'LOGPATH' = Path where log files will be saved." + nl +
                "'DATAPATH' = Path where temporary data files will be saved." + nl +
                "'CODEXDEPLOYMENT' = Path to codex-deployment JSON file." + nl +
                nl);
        }

        private void Validate(Configuration configuration)
        {
            if (string.IsNullOrEmpty(configuration.LogPath))
            {
                throw new Exception($"Invalid LogPath set: '{configuration.LogPath}'");
            }

            if (string.IsNullOrEmpty(configuration.DataPath))
            {
                throw new Exception($"Invalid DataPath set: '{configuration.DataPath}'");
            }

            if (configuration.CodexDeployment == null || !configuration.CodexDeployment.CodexContainers.Any())
            {
                throw new Exception("No Codex deployment found.");
            }
        }

        private CodexDeployment ParseCodexDeploymentJson(string filename)
        {
            var d = JsonConvert.DeserializeObject<CodexDeployment>(File.ReadAllText(filename))!;
            if (d == null) throw new Exception("Unable to parse " + filename);
            return d;
        }
    }
}
