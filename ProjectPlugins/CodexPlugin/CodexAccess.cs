using Core;
using KubernetesWorkflow;
using KubernetesWorkflow.Types;
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
            return Http().HttpGetStream("data/" + contentId + "/network");
        }

        public CodexLocalDataResponse[] LocalFiles()
        {
            return Http().HttpGetJson<CodexLocalDataResponse[]>("data");
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
            var address = GetAddress();
            var api = new CodexOpenApi.CodexApi(new HttpClient());
            api.BaseUrl = $"{address.Host}:{address.Port}/api/codex/v1";

            var debugInfo = Time.Wait(api.GetDebugInfoAsync());

            using var stream = File.OpenRead("C:\\Users\\thatb\\Desktop\\Collect\\Wallpapers\\demerui_djinn_illuminatus_fullbody_full_body_view_in_the_style__86ea9491-1fe1-44ab-8577-a3636cad1b21.png");
            var cid = Time.Wait(api.UploadAsync(stream));

            var file = Time.Wait(api.DownloadNetworkAsync(cid));
            while (file.IsPartial) Thread.Sleep(100);
            using var outfile = File.OpenWrite("C:\\Users\\thatb\\Desktop\\output.png");
            file.Stream.CopyTo(outfile);

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
