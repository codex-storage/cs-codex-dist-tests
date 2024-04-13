using CodexOpenApi;
using Core;
using KubernetesWorkflow;
using KubernetesWorkflow.Types;
using Newtonsoft.Json;
using Utils;

namespace CodexPlugin
{
    public class CodexAccess : ILogHandler
    {
        private readonly IPluginTools tools;
        private readonly Mapper mapper = new Mapper();
        private bool hasContainerCrashed;

        public CodexAccess(IPluginTools tools, RunningPod container, CrashWatcher crashWatcher)
        {
            this.tools = tools;
            Container = container;
            CrashWatcher = crashWatcher;
            hasContainerCrashed = false;

            CrashWatcher.Start(this);
        }

        public RunningPod Container { get; }
        public CrashWatcher CrashWatcher { get; }

        public DebugInfo GetDebugInfo()
        {
            return mapper.Map(OnCodex(api => api.GetDebugInfoAsync()));
        }

        public DebugPeer GetDebugPeer(string peerId)
        {
            // Cannot use openAPI: debug/peer endpoint is not specified there.
            var endpoint = GetEndpoint();
            var str = endpoint.HttpGetString($"debug/peer/{peerId}");

            if (str.ToLowerInvariant() == "unable to find peer!")
            {
                return new DebugPeer
                {
                    IsPeerFound = false
                };
            }

            var result = endpoint.Deserialize<DebugPeer>(str);
            result.IsPeerFound = true;
            return result;
        }

        public void ConnectToPeer(string peerId, string[] peerMultiAddresses)
        {
            OnCodex(api =>
            {
                Time.Wait(api.ConnectPeerAsync(peerId, peerMultiAddresses));
                return Task.FromResult(string.Empty);
            });
        }

        public string UploadFile(FileStream fileStream)
        {
            return OnCodex(api => api.UploadAsync(fileStream));
        }

        public Stream DownloadFile(string contentId)
        {
            var fileResponse = OnCodex(api => api.DownloadNetworkAsync(contentId));
            if (fileResponse.StatusCode != 200) throw new Exception("Download failed with StatusCode: " + fileResponse.StatusCode);
            return fileResponse.Stream;
        }

        public LocalDatasetList LocalFiles()
        {
            return mapper.Map(OnCodex(api => api.ListDataAsync()));
        }

        public StorageAvailability SalesAvailability(StorageAvailability request)
        {
            var body = mapper.Map(request);
            var read = OnCodex<SalesAvailabilityREAD>(api => api.OfferStorageAsync(body));
            return mapper.Map(read);
        }

        public string RequestStorage(StoragePurchaseRequest request)
        {
            var body = mapper.Map(request);
            return OnCodex<string>(api => api.CreateStorageRequestAsync(request.ContentId.Id, body));
        }

        public StoragePurchase GetPurchaseStatus(string purchaseId)
        {
            var endpoint = GetEndpoint();
            return Time.Retry(() =>
            {
                var str = endpoint.HttpGetString($"storage/purchases/{purchaseId}");
                if (string.IsNullOrEmpty(str)) throw new Exception("Empty response.");
                return JsonConvert.DeserializeObject<StoragePurchase>(str)!;
            }, nameof(GetPurchaseStatus));

            // TODO: current getpurchase api does not line up with its openapi spec.
            // return mapper.Map(OnCodex(api => api.GetPurchaseAsync(purchaseId)));
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

        private T OnCodex<T>(Func<CodexApi, Task<T>> action)
        {
            var address = GetAddress();
            var result = tools.CreateHttp(CheckContainerCrashed)
                .OnClient(client =>
            {
                var api = new CodexApi(client);
                api.BaseUrl = $"{address.Host}:{address.Port}/api/codex/v1";
                return Time.Wait(action(api));
            });
            return result;
        }

        private IEndpoint GetEndpoint()
        {
            return tools
                .CreateHttp(CheckContainerCrashed)
                .CreateEndpoint(GetAddress(), "/api/codex/v1/", Container.Name);
        }

        private Address GetAddress()
        {
            return Container.Containers.Single().GetAddress(tools.GetLog(), CodexContainerRecipe.ApiPortTag);
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
