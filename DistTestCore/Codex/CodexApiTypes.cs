using KubernetesWorkflow;
using Logging;
using Newtonsoft.Json;
using Utils;

namespace DistTestCore.Codex
{
    public class CodexDebugResponse
    {
        public string id { get; set; } = string.Empty;
        public string[] addrs { get; set; } = new string[0];
        public string repo { get; set; } = string.Empty;
        public string spr { get; set; } = string.Empty;
        public EnginePeerResponse[] enginePeers { get; set; } = Array.Empty<EnginePeerResponse>();
        public SwitchPeerResponse[] switchPeers { get; set; } = Array.Empty<SwitchPeerResponse>();
        public CodexDebugVersionResponse codex { get; set; } = new();
        public CodexDebugTableResponse table { get; set; } = new();
    }

    public class CodexDebugFutures
    {
        public int futures { get; set; }
    }

    public class CodexDebugTableResponse
    {
        public CodexDebugTableNodeResponse localNode { get; set; } = new();
        public CodexDebugTableNodeResponse[] nodes { get; set; } = Array.Empty<CodexDebugTableNodeResponse>();
    }

    public class CodexDebugTableNodeResponse
    {
        public string nodeId { get; set; } = string.Empty;
        public string peerId { get; set; } = string.Empty;
        public string record { get; set; } = string.Empty;
        public string address { get; set; } = string.Empty;
        public bool seen { get; set; }
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

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(version) && !string.IsNullOrEmpty(revision);
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class CodexDebugPeerResponse
    {
        public bool IsPeerFound { get; set; }

        public string peerId { get; set; } = string.Empty;
        public long seqNo { get; set; }
        public CodexDebugPeerAddressResponse[] addresses { get; set; } = Array.Empty<CodexDebugPeerAddressResponse>();
    }

    public class CodexDebugPeerAddressResponse
    {
        public string address { get; set; } = string.Empty;
    }

    public class CodexDebugThresholdBreaches
    {
        public string[] breaches { get; set; } = Array.Empty<string>();
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
        public uint? tolerance { get; set; }
    }

    public class CodexStoragePurchase
    {
        public string state { get; set; } = string.Empty;
        public string error { get; set; } = string.Empty;
    }
}
