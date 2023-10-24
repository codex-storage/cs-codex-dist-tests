using CodexPlugin;
using Discord;
using Newtonsoft.Json;

namespace BiblioTech
{
    public class DeploymentsFilesMonitor
    {
        private DateTime lastUpdate = DateTime.MinValue;
        private CodexDeployment[] deployments = Array.Empty<CodexDeployment>();

        public CodexDeployment[] GetDeployments()
        {
            if (ShouldUpdate()) UpdateDeployments();

            return deployments;
        }

        public async Task<bool> DownloadDeployment(IAttachment file)
        {
            using var http = new HttpClient();
            var response = await http.GetAsync(file.Url);
            var str = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(str)) return false;

            try
            {
                var deploy = JsonConvert.DeserializeObject<CodexDeployment>(str);
                if (deploy != null)
                {
                    var targetFile = Path.Combine(Program.Config.EndpointsPath, Guid.NewGuid().ToString().ToLowerInvariant() + ".json");
                    File.WriteAllText(targetFile, str);
                    deployments = Array.Empty<CodexDeployment>();
                    return true;
                }
            }
            catch { }
            return false;
        }

        public bool DeleteDeployment(string deploymentName)
        {
            var path = Program.Config.EndpointsPath;
            if (!Directory.Exists(path)) return false;
            var files = Directory.GetFiles(path);

            foreach ( var file in files)
            {
                var deploy = ProcessFile(file);
                if (deploy != null && deploy.Metadata.Name == deploymentName)
                {
                    File.Delete(file);
                    deployments = Array.Empty<CodexDeployment>();
                    return true;
                }
            }
            return false;
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
