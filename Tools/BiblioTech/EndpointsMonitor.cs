using CodexPlugin;
using Core;
using KubernetesWorkflow;

namespace BiblioTech
{
    public class EndpointsMonitor
    {
        private readonly EndpointsFilesMonitor fileMonitor;
        private readonly EntryPoint entryPoint;
        private DateTime lastUpdate = DateTime.MinValue;
        private string report = string.Empty;

        public EndpointsMonitor(EndpointsFilesMonitor fileMonitor, EntryPoint entryPoint)
        {
            this.fileMonitor = fileMonitor;
            this.entryPoint = entryPoint;
        }

        public async Task<string> GetReport()
        {
            if (ShouldUpdate())
            {
                await UpdateReport();
            }

            return report;
        }

        private async Task UpdateReport()
        {
            lastUpdate = DateTime.UtcNow;

            var endpoints = fileMonitor.GetEndpoints();
            if (!endpoints.Any())
            {
                report = "There are no networks currently online.";
            }
            else
            {
                var nl = Environment.NewLine;
                report = $"There are {endpoints.Length} networks online." + nl;
                for (var i = 0; i < endpoints.Length; i++)
                {
                    var e = endpoints[i];
                    report += $" [{i} - {e.Name}] = {await GetStatusMessage(e)}";
                }
            }
        }

        private async Task<string> GetStatusMessage(CodexEndpoints e)
        {
            var success = 0;
            foreach (var addr in e.Addresses)
            {
                await Task.Run(() =>
                {
                    // this is definitely not going to work:
                    var rc = new RunningContainer(null!, null!, null!, "", addr, addr);

                    var access = new CodexAccess(entryPoint.Tools, rc, null!);
                    var debugInfo = access.GetDebugInfo();

                    if (!string.IsNullOrEmpty(debugInfo.id))
                    {
                        success++;
                    }
                });
            }

            return $"{success} / {e.Addresses.Length} online.";
            // todo: analyze returned peerIDs to say something about
            // the number of none-test-net managed nodes seen on the network.
        }

        private bool ShouldUpdate()
        {
            return string.IsNullOrEmpty(report) || (DateTime.UtcNow - lastUpdate) > TimeSpan.FromMinutes(10);
        }
    }
}
