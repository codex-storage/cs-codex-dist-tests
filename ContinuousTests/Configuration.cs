using DistTestCore.Codex;
using Newtonsoft.Json;

namespace ContinuousTests
{
    public class Configuration
    {
        public string LogPath { get; set; } = string.Empty;
        public CodexDeployment CodexDeployment { get; set; } = null!;
        public int SleepSecondsPerSingleTest { get; set; }
        public int SleepSecondsPerAllTests { get; set; }
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
            var codexDeploymentJson = Environment.GetEnvironmentVariable("CODEXDEPLOYMENT");
            var sleepPerSingle = Environment.GetEnvironmentVariable("SLEEPSECONDSPERSINGLETEST");
            var sleepPerAll = Environment.GetEnvironmentVariable("SLEEPSECONDSPERALLTESTS");
            var keep = Environment.GetEnvironmentVariable("KEEPPASSEDTESTLOGS");

            if (!string.IsNullOrEmpty(logPath) &&
                !string.IsNullOrEmpty(codexDeploymentJson) &&
                !string.IsNullOrEmpty(sleepPerSingle) &&
                !string.IsNullOrEmpty(sleepPerAll))
            {
                try 
                {
                    return new Configuration
                    { 
                        LogPath = logPath,
                        CodexDeployment = ParseCodexDeploymentJson(codexDeploymentJson),
                        SleepSecondsPerSingleTest = Convert.ToInt32(sleepPerSingle),
                        SleepSecondsPerAllTests = Convert.ToInt32(sleepPerAll),
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
                "'CODEXDEPLOYMENT' = Path to codex-deployment JSON file." + nl +
                "'SLEEPSECONDSPERSINGLETEST' = Seconds to sleep after each individual test." + nl +
                "'SLEEPSECONDSPERALLTESTS' = Seconds to sleep after all tests, before starting again." + nl +
                "'KEEPPASSEDTESTLOGS' = (Optional, default: 0) Set to '1' to keep log files of tests that passed." + nl +
                nl);
        }

        private void Validate(Configuration configuration)
        {
            if (configuration.SleepSecondsPerSingleTest < 1)
            {
                Console.WriteLine("Warning: configuration.SleepSecondsPerSingleTest was less than 1 seconds. Using 1 seconds instead!");
                configuration.SleepSecondsPerSingleTest = 1;
            }
            if (configuration.SleepSecondsPerAllTests < 1)
            {
                Console.WriteLine("Warning: configuration.SleepSecondsPerAllTests was less than 10 seconds. Using 10 seconds instead!");
                configuration.SleepSecondsPerAllTests = 10;
            }

            if (string.IsNullOrEmpty(configuration.LogPath))
            {
                throw new Exception($"Unvalid logpath set: '{configuration.LogPath}'");
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
