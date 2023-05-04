using KubernetesWorkflow;
using Logging;

namespace DistTestCore.Codex
{
    public class CodexAccess
    {
        private readonly BaseLog log;
        private readonly ITimeSet timeSet;

        public CodexAccess(BaseLog log, ITimeSet timeSet, RunningContainer runningContainer)
        {
            this.log = log;
            this.timeSet = timeSet;
            Container = runningContainer;
        }

        public RunningContainer Container { get; }

        public CodexDebugResponse GetDebugInfo()
        {
            return Http().HttpGetJson<CodexDebugResponse>("debug/info");
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

        public void EnsureOnline()
        {
            try
            {
                var debugInfo = GetDebugInfo();
                if (debugInfo == null || string.IsNullOrEmpty(debugInfo.id)) throw new InvalidOperationException("Unable to get debug-info from codex node at startup.");

                var nodePeerId = debugInfo.id;
                var nodeName = Container.Name;
                log.AddStringReplace(nodePeerId, $"___{nodeName}___");
            }
            catch (Exception e)
            {
                log.Error($"Failed to start codex node: {e}. Test infra failure.");
                throw new InvalidOperationException($"Failed to start codex node. Test infra failure.", e);
            }
        }

        private Http Http()
        {
            var ip = Container.Pod.Cluster.IP;
            var port = Container.ServicePorts[0].Number;
            return new Http(log, timeSet, ip, port, baseUrl: "/api/codex/v1");
        }

        public string ConnectToPeer(string peerId, string peerMultiAddress)
        {
            return Http().HttpGetString($"connect/{peerId}?addrs={peerMultiAddress}");
        }
    }

    public class CodexDebugResponse
    {
        public string id { get; set; } = string.Empty;
        public string[] addrs { get; set; } = new string[0];
        public string repo { get; set; } = string.Empty;
        public string spr { get; set; } = string.Empty;
        public EnginePeerResponse[] enginePeers { get; set; } = Array.Empty<EnginePeerResponse>();
        public SwitchPeerResponse[] switchPeers { get; set; } = Array.Empty<SwitchPeerResponse>();
        public CodexDebugVersionResponse codex { get; set; } = new();
    }

    public class EnginePeerResponse
    {
        public string peerId { get; set; } = string.Empty;
        public EnginePeerContextResponse context { get; set; } = new();
    }

    public class EnginePeerContextResponse
    {
        public int blocks { get; set; } = 0;
        public int peerWants { get; set; } = 0;
        public int exchanged { get; set; } = 0;
        public string lastExchange { get; set; } = string.Empty;
    }

    public class SwitchPeerResponse
    {
        public string peerId { get; set; } = string.Empty;
        public string key { get; set; } = string.Empty;
    }

    public class CodexDebugVersionResponse
    {
        public string version { get; set; } = string.Empty;
        public string revision { get; set; } = string.Empty;
    }

    public class CodexSalesAvailabilityRequest
    {
        public string size { get; set; } = string.Empty;
        public string duration { get; set; } = string.Empty;
        public string minPrice { get; set; } = string.Empty;
        public string maxCollateral { get; set; } = string.Empty;
    }

    public class CodexSalesAvailabilityResponse
    {
        public string id { get; set; } = string.Empty;
        public string size { get; set; } = string.Empty;
        public string duration { get; set; } = string.Empty;
        public string minPrice { get; set; } = string.Empty;
        public string maxCollateral { get; set; } = string.Empty;
    }

    public class CodexSalesRequestStorageRequest
    {
        public string duration { get; set; } = string.Empty;
        public string proofProbability { get; set; } = string.Empty;
        public string reward { get; set; } = string.Empty;
        public string collateral { get; set; } = string.Empty;
        public string? expiry { get; set; }
        public uint? nodes { get; set; }
        public uint? tolerance { get; set;}
    }
}
