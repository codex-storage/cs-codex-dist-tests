using KubernetesWorkflow;
using Logging;
using Utils;

namespace DistTestCore.Codex
{
    public class CodexAccess
    {
        private readonly BaseLog log;
        private readonly ITimeSet timeSet;

        public CodexAccess(BaseLog log, RunningContainer container, ITimeSet timeSet, Address address)
        {
            this.log = log;
            Container = container;
            this.timeSet = timeSet;
            Address = address;
        }

        public RunningContainer Container { get; }
        public Address Address { get; }

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

        public int GetDebugFutures()
        {
            return Http().HttpGetJson<CodexDebugFutures>("debug/futures").futures;
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

        private Http Http()
        {
            return new Http(log, timeSet, Address, baseUrl: "/api/codex/v1", Container.Name);
        }
    }
}
