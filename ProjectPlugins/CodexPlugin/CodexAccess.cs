using Core;
using KubernetesWorkflow;
using Utils;

namespace CodexPlugin
{
    public class CodexAccess : ILogHandler
    {
        private readonly IPluginTools tools;
        private bool hasContainerCrashed;

        public CodexAccess(IPluginTools tools, RunningContainer container, CrashWatcher crashWatcher)
        {
            this.tools = tools;
            Container = container;
            CrashWatcher = crashWatcher;
            hasContainerCrashed = false;

            CrashWatcher.Start(this);
        }

        public RunningContainer Container { get; }
        public CrashWatcher CrashWatcher { get; }

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

            var result = http.Deserialize<CodexDebugPeerResponse>(str);
            result.IsPeerFound = true;
            return result;
        }

        public CodexDebugBlockExchangeResponse GetDebugBlockExchange()
        {
            return Http().HttpGetJson<CodexDebugBlockExchangeResponse>("debug/blockexchange");
        }

        public CodexDebugRepoStoreResponse[] GetDebugRepoStore()
        {
            return LongHttp().HttpGetJson<CodexDebugRepoStoreResponse[]>("debug/repostore");
        }

        public CodexDebugThresholdBreaches GetDebugThresholdBreaches()
        {
            return Http().HttpGetJson<CodexDebugThresholdBreaches>("debug/loop");
        }

        public string UploadFile(FileStream fileStream)
        {
            return Http().HttpPostStream("data", fileStream);
        }

        public Stream DownloadFile(string contentId)
        {
            return Http().HttpGetStream("data/" + contentId);
        }

        public CodexLocalDataResponse[] LocalFiles()
        {
            return Http().HttpGetJson<CodexLocalDataResponse[]>("local");
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

        public PodInfo GetPodInfo()
        {
            var workflow = tools.CreateWorkflow();
            return workflow.GetPodInfo(Container);
        }

        private IHttp Http()
        {
            return tools.CreateHttp(GetAddress(), baseUrl: "/api/codex/v1", CheckContainerCrashed, Container.Name);
        }

        private IHttp LongHttp()
        {
            return tools.CreateHttp(GetAddress(), baseUrl: "/api/codex/v1", CheckContainerCrashed, new LongTimeSet(), Container.Name);
        }

        private Address GetAddress()
        {
            return Container.GetAddress(tools.GetLog(), CodexContainerRecipe.ApiPortTag);
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
            file.Write($"Container Crash Log for {Container.Name}.");

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
