using KubernetesWorkflow;
using Logging;
using Utils;

namespace DistTestCore.Codex
{
    public class CodexAccess : ILogHandler
    {
        private readonly BaseLog log;
        private readonly ITimeSet timeSet;
        private bool hasContainerCrashed;

        public CodexAccess(BaseLog log, RunningContainer container, ITimeSet timeSet, Address address)
        {
            this.log = log;
            Container = container;
            this.timeSet = timeSet;
            Address = address;
            hasContainerCrashed = false;

            if (container.CrashWatcher != null) container.CrashWatcher.Start(this);
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
            // Some Codex images support debug/futures to count the number of open futures.
            return 0; // Http().HttpGetJson<CodexDebugFutures>("debug/futures").futures;
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
            return Http().HttpPostJson<CodexSalesRequestStorageRequest, string>($"storage/request/{contentId}", request);
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

        private Http Http()
        {
            return new Http(log, timeSet, Address, baseUrl: "/api/codex/v1", CheckContainerCrashed, Container.Name);
        }

        private void CheckContainerCrashed(HttpClient client)
        {
            if (hasContainerCrashed) throw new Exception("Container has crashed.");
        }

        public void Log(Stream crashLog)
        {
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
