using Core;
using KubernetesWorkflow;

namespace CodexPlugin
{
    public class CodexAccess : ILogHandler
    {
        private readonly IPluginTools tools;
        private bool hasContainerCrashed;

        public CodexAccess(IPluginTools tools, RunningContainer container)
        {
            this.tools = tools;
            Container = container;
            hasContainerCrashed = false;

            if (container.CrashWatcher != null) container.CrashWatcher.Start(this);
        }

        public RunningContainer Container { get; }

        public CodexDebugResponse GetDebugInfo()
        {
            return Http().HttpGetJson<CodexDebugResponse>("debug/info");
        }

        public CodexDebugPeerResponse GetDebugPeer(string peerId)
        {
            var http = Http();
            var str = http.HttpGetString($"debug/peer/{peerId}");

            if (str.ToLowerInvariant() == "unable to find peer!")
            {
                return new CodexDebugPeerResponse
                {
                    IsPeerFound = false
                };
            }

            var result = http.TryJsonDeserialize<CodexDebugPeerResponse>(str);
            result.IsPeerFound = true;
            return result;
        }

        public CodexDebugThresholdBreaches GetDebugThresholdBreaches()
        {
            return Http().HttpGetJson<CodexDebugThresholdBreaches>("debug/loop");
        }

        public string UploadFile(FileStream fileStream)
        {
            return Http().HttpPostStream("upload", fileStream);
        }

        public Stream DownloadFile(string contentId)
        {
            return Http().HttpGetStream("download/" + contentId);
        }

        public CodexSalesAvailabilityResponse SalesAvailability(CodexSalesAvailabilityRequest request)
        {
            return Http().HttpPostJson<CodexSalesAvailabilityRequest, CodexSalesAvailabilityResponse>("sales/availability", request);
        }

        public string RequestStorage(CodexSalesRequestStorageRequest request, string contentId)
        {
            return Http().HttpPostJson($"storage/request/{contentId}", request);
        }

        public CodexStoragePurchase GetPurchaseStatus(string purchaseId)
        {
            return Http().HttpGetJson<CodexStoragePurchase>($"storage/purchases/{purchaseId}");
        }

        public string ConnectToPeer(string peerId, string peerMultiAddress)
        {
            return Http().HttpGetString($"connect/{peerId}?addrs={peerMultiAddress}");
        }

        public string GetName()
        {
            return Container.Name;
        }

        private IHttp Http()
        {
            return tools.CreateHttp(Container.Address, baseUrl: "/api/codex/v1", CheckContainerCrashed, Container.Name);
        }

        private void CheckContainerCrashed(HttpClient client)
        {
            if (hasContainerCrashed) throw new Exception("Container has crashed.");
        }

        public void Log(Stream crashLog)
        {
            var log = tools.GetLog();
            var file = log.CreateSubfile();
            log.Log($"Container {Container.Name} has crashed. Downloading crash log to '{file.FullFilename}'...");

            using var reader = new StreamReader(crashLog);
            var line = reader.ReadLine();
            while (line != null)
            {
                file.Write(line);
                line = reader.ReadLine();
            }

            log.Log("Crash log successfully downloaded.");
            hasContainerCrashed = true;
        }
    }
}
