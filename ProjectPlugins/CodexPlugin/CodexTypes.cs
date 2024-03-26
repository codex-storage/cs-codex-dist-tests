namespace CodexPlugin
{
    public class DebugInfo
    {
        public string[] Addrs { get; set; } = Array.Empty<string>();
        public string Spr { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string[] AnnounceAddresses { get; set; } = Array.Empty<string>();
    }

    public class DebugPeer
    {
        public bool IsPeerFound { get; set; }

    }

    public class LocalDataset
    {
        public ContentId Cid { get; set; } = new ContentId();
    }

    public class DebugVersion
    {
        public string Version { get; internal set; } = string.Empty;
        public string Revision { get; internal set; } = string.Empty;
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

        public override bool Equals(object? obj)
        {
            return obj is ContentId id && Id == id.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }
    }
}
