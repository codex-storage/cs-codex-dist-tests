using CodexPlugin;
using Newtonsoft.Json;

namespace BiblioTech
{
    public class DeploymentsFilesMonitor
    {
        private DateTime lastUpdate = DateTime.MinValue;
        private CodexDeployment[] deployments = Array.Empty<CodexDeployment>();

        public CodexDeployment[] GetDeployments()
        {
            if (ShouldUpdate())
            {
                UpdateDeployments();
            }

            return deployments;
        }

        private void UpdateDeployments()
        {
            lastUpdate = DateTime.UtcNow;
            var path = Program.Config.EndpointsPath;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                File.WriteAllText(Path.Combine(path, "readme.txt"), "Place codex-deployment.json here.");
                return;
            }

            var files = Directory.GetFiles(path);
            deployments = files.Select(ProcessFile).Where(d => d != null).Cast<CodexDeployment>().ToArray();
        }

        private CodexDeployment? ProcessFile(string filename)
        {
            try
            {
                var lines = string.Join(" ", File.ReadAllLines(filename));
                return JsonConvert.DeserializeObject<CodexDeployment>(lines);
            }
            catch
            {
                return null;
            }
        }

        private bool ShouldUpdate()
        {
            return !deployments.Any() || (DateTime.UtcNow - lastUpdate) > TimeSpan.FromMinutes(10);
        }
    }
}
