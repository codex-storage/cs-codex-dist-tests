using Newtonsoft.Json;
using Utils;

namespace CodexPlugin
{
    public class DebugInfo
    {
        public string[] Addrs { get; set; } = Array.Empty<string>();
        public string Spr { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string[] AnnounceAddresses { get; set; } = Array.Empty<string>();
        public DebugInfoVersion Version { get; set; } = new();
        public DebugInfoTable Table { get; set; } = new();
    }

    public class DebugInfoVersion
    {
        public string Version { get; set; } = string.Empty;
        public string Revision { get; set; } = string.Empty;

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Version) && !string.IsNullOrEmpty(Revision);
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class DebugInfoTable
    {
        public DebugInfoTableNode LocalNode { get; set; } = new();
        public DebugInfoTableNode[] Nodes { get; set; } = Array.Empty<DebugInfoTableNode>();
    }

    public class DebugInfoTableNode
    {
        public string NodeId { get; set; } = string.Empty;
        public string PeerId { get; set; } = string.Empty;
        public string Record { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool Seen { get; set; }
    }

    public class DebugPeer
    {
        public bool IsPeerFound { get; set; }
        public string PeerId { get; set; } = string.Empty;
        public string[] Addresses { get; set; } = Array.Empty<string>();
    }

    public class LocalDatasetList
    {
        public LocalDataset[] Content { get; set; } = Array.Empty<LocalDataset>();
    } 

    public class LocalDataset
    {
        public ContentId Cid { get; set; } = new();
        public Manifest Manifest { get; set; } = new();
    }

    public class Manifest
    {
        public string RootHash { get; set; } = string.Empty;
        public ByteSize OriginalBytes { get; set; } = ByteSize.Zero;
        public ByteSize BlockSize { get; set; } = ByteSize.Zero;
        public bool Protected { get; set; }
    }

    public class SalesRequestStorageRequest
    {
        public string Duration { get; set; } = string.Empty;
        public string ProofProbability { get; set; } = string.Empty;
        public string Reward { get; set; } = string.Empty;
        public string Collateral { get; set; } = string.Empty;
        public string? Expiry { get; set; }
        public uint? Nodes { get; set; }
        public uint? Tolerance { get; set; }
    }

    public class ContentId
    {
        public ContentId()
        {
            Id = string.Empty;
        }

        public ContentId(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public override string ToString()
        {
            return Id;
        }

        public override bool Equals(object? obj)
        {
            return obj is ContentId id && Id == id.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }
    }

    public class CodexSpace
    {
        public long TotalBlocks { get; set; }
        public long QuotaMaxBytes { get; set; }
        public long QuotaUsedBytes { get; set; }
        public long QuotaReservedBytes { get; set; }
        public long FreeBytes => QuotaMaxBytes - (QuotaUsedBytes + QuotaReservedBytes);

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
