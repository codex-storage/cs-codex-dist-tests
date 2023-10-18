using CodexPlugin;
using KubernetesWorkflow;
using Newtonsoft.Json;
using Utils;

namespace BiblioTech
{
    public class EndpointsFilesMonitor
    {
        private DateTime lastUpdate = DateTime.MinValue;
        private CodexEndpoints[] endpoints = Array.Empty<CodexEndpoints>();

        public CodexEndpoints[] GetEndpoints()
        {
            if (ShouldUpdate())
            {
                UpdateEndpoints();
            }

            return endpoints;
        }

        private void UpdateEndpoints()
        {
            lastUpdate = DateTime.UtcNow;
            var path = Program.Config.EndpointsPath;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                File.WriteAllText(Path.Combine(path, "readme.txt"), "Place codex-deployment.json or codex-endpoints.json here.");
                return;
            }

            var files = Directory.GetFiles(path);
            endpoints = files.Select(ProcessFile).Where(d => d != null).Cast<CodexEndpoints>().ToArray();
        }

        private CodexEndpoints? ProcessFile(string filename)
        {
            try
            {
                var lines = string.Join(" ", File.ReadAllLines(filename));
                try
                {
                    return JsonConvert.DeserializeObject<CodexEndpoints>(lines);
                }
                catch { }

                return ConvertToEndpoints(JsonConvert.DeserializeObject<CodexDeployment>(lines));
            }
            catch
            {
                return null;
            }
        }

        private CodexEndpoints? ConvertToEndpoints(CodexDeployment? codexDeployment)
        {
            if (codexDeployment == null) return null;

            return new CodexEndpoints
            {
                Name = "codex-deployment-" + codexDeployment.Metadata.StartUtc.ToString("o"),
                Addresses = codexDeployment.CodexContainers.Select(ConvertToAddress).ToArray()
            };
        }

        private Address ConvertToAddress(RunningContainer rc)
        {
            return rc.ClusterExternalAddress;
        }

        private bool ShouldUpdate()
        {
            return !endpoints.Any() || (DateTime.UtcNow - lastUpdate) > TimeSpan.FromMinutes(10);
        }
    }
}
