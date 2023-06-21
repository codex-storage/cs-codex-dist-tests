using Newtonsoft.Json;

namespace ContinuousTests
{
    public class Configuration
    {
        public string LogPath { get; set; } = string.Empty;
        public string[] CodexUrls { get; set; } = Array.Empty<string>();
        public int SleepSecondsPerTest { get; set; }
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
            var codexUrls = Environment.GetEnvironmentVariable("CODEXURLS");
            var sleep = Environment.GetEnvironmentVariable("SLEEPSECONDSPERTEST");

            if (!string.IsNullOrEmpty(logPath) && !string.IsNullOrEmpty(codexUrls) && !string.IsNullOrEmpty(sleep))
            {
                var urls = codexUrls.Split(';', StringSplitOptions.RemoveEmptyEntries);
                int ms;
                if (int.TryParse(sleep, out ms))
                {
                    if (urls.Length > 0)
                    {
                        return new Configuration { LogPath = logPath, CodexUrls = urls, SleepSecondsPerTest = ms };
                    }
                }
            }

            throw new Exception($"Unable to load configuration from '{filename}', and " +
                $"unable to load configuration from environment variables 'LOGPATH' and 'CODEXURLS', and 'SLEEPSECONDSPERTEST'. " +
                $"(semi-colon-separated URLs) " +
                $"Create the configuration file or set the environment veriables.");
        }

        private void Validate(Configuration configuration)
        {
            if (configuration.SleepSecondsPerTest < 10)
            {
                Console.WriteLine("Warning: configuration.SleepMsPerTest was less than 10 seconds. Using 10 seconds instead!");
                configuration.SleepSecondsPerTest = 10;
            }

            if (string.IsNullOrEmpty(configuration.LogPath))
            {
                throw new Exception($"Unvalid logpath set: '{configuration.LogPath}'");
            }

            if (!configuration.CodexUrls.Any())
            {
                throw new Exception("No Codex URLs found.");
            }
        }
    }
}
