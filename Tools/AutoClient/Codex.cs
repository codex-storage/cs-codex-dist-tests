using CodexOpenApi;
using CodexPlugin;
using Core;
using Utils;
using DebugInfo = CodexPlugin.DebugInfo;

namespace AutoClient
{
    public class Codex
    {
        private readonly IPluginTools tools;
        private readonly Address address;
        private readonly Mapper mapper = new Mapper();

        /// <summary>
        /// This class was largely copied from CodexAccess in CodexPlugin.
        /// Should really be generalized so CodexPlugin supports talking to custom Codex instances.
        /// </summary>
        public Codex(IPluginTools tools, Address address)
        {
            this.tools = tools;
            this.address = address;
        }

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
            return mapper.Map(OnCodex(api => api.GetPurchaseAsync(purchaseId)));
        }

        public string GetPurchaseStatusRaw(string purchaseId)
        {
            var endpoint = GetEndpoint();
            return endpoint.HttpGetString($"storage/purchases/{purchaseId}");
        }

        private T OnCodex<T>(Func<CodexApi, Task<T>> action)
        {
            var result = tools.CreateHttp()
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
                .CreateHttp()
                .CreateEndpoint(address, "/api/codex/v1/");
        }
    }
}
