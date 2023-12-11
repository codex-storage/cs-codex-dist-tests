using CodexPlugin;
using Discord;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace BiblioTech
{
    public class DeploymentsFilesMonitor
    {
        private readonly List<CodexDeployment> deployments = new List<CodexDeployment>();

        public void Initialize()
        {
            LoadDeployments();
        }

        public CodexDeployment[] GetDeployments()
        {
            return deployments.ToArray();
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
                var names = deployments.Select(d => d.Metadata.Name).ToArray();
                if (deploy != null && IsDeploymentOk(deploy) && !names.Contains(deploy.Metadata.Name))
                {
                    var targetFile = Path.Combine(Program.Config.EndpointsPath, Guid.NewGuid().ToString().ToLowerInvariant() + ".json");
                    File.WriteAllText(targetFile, str);
                    LoadDeployments();
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

            foreach (var file in files)
            {
                var deploy = ProcessFile(file);
                if (deploy != null && deploy.Metadata.Name == deploymentName)
                {
                    File.Delete(file);
                    LoadDeployments();
                    return true;
                }
            }
            return false;
        }

        private bool IsDeploymentOk(CodexDeployment? deploy)
        {
            if (deploy == null) return false;
            if (deploy.GethDeployment == null) return false;
            if (deploy.GethDeployment.Containers == null) return false;
            return true;
        }

        private void LoadDeployments()
        {
            deployments.Clear();
            var path = Program.Config.EndpointsPath;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                File.WriteAllText(Path.Combine(path, "readme.txt"), "Place codex-deployment.json here.");
                return;
            }

            var files = Directory.GetFiles(path);
            deployments.AddRange(files
                .Select(ProcessFile)
                .Where(d => d != null)
                .Cast<CodexDeployment>()
                .Distinct(new DeploymentNameEqual()));
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
    }

    internal class DeploymentNameEqual : IEqualityComparer<CodexDeployment>
    {
        public bool Equals(CodexDeployment? x, CodexDeployment? y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            return x.Metadata.Name == y.Metadata.Name;
        }

        public int GetHashCode([DisallowNull] CodexDeployment obj)
        {
            return obj.Metadata.Name.GetHashCode();
        }
    }
}