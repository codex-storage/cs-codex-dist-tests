namespace AutoClient
{
    public class CidRepo
    {
        private readonly Random random = new Random();
        private readonly object _lock = new object();
        private readonly List<CidEntry> entries = new List<CidEntry>();
        private readonly Configuration config;

        public CidRepo(Configuration config)
        {
            this.config = config;
        }

        public void Add(string nodeId, string cid, long knownSize)
        {
            lock (_lock)
            {
                entries.Add(new CidEntry(nodeId, cid, knownSize));
            }
        }

        public void AddEncoded(string originalCid, string encodedCid)
        {
            lock (_lock)
            {
                var entry = entries.SingleOrDefault(e => e.Cid == originalCid);
                if (entry == null) return;

                entry.Encoded = encodedCid;
            }
        }

        public string? GetForeignCid(string myNodeId)
        {
            lock (_lock)
            {
                while (true)
                {
                    if (!entries.Any()) return null;
                    var available = entries.Where(e => e.NodeId != myNodeId).ToArray();
                    if (!available.Any()) return null;

                    var i = random.Next(0, available.Length);
                    var entry = available[i];

                    if (entry.CreatedUtc < (DateTime.UtcNow + TimeSpan.FromMinutes(config.ContractDurationMinutes)))
                    {
                        entries.Remove(entry);
                    }
                    else
                    {
                        return entry.Cid;
                    }
                }
            }
        }

        public long? GetSizeForCid(string cid)
        {
            lock (_lock)
            {
                var entry = entries.SingleOrDefault(e => e.Cid == cid);
                if (entry == null) return null;
                return entry.KnownSize;
            }
        }
    }

    public class CidEntry
    {
        public CidEntry(string nodeId, string cid, long knownSize)
        {
            NodeId = nodeId;
            Cid = cid;
            KnownSize = knownSize;
        }

        public string NodeId { get; }
        public string Cid { get; }
        public string Encoded { get; set; } = string.Empty;
        public long KnownSize { get; }
        public DateTime CreatedUtc { get; } = DateTime.UtcNow;
    }
}
