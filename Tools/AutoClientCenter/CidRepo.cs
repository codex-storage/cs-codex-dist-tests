namespace AutoClientCenter
{
    public class CidRepo
    {
        private readonly Random random = new Random();
        private readonly object _lock = new object();
        private readonly List<CidEntry> entries = new List<CidEntry>();

        public void Add(string cid, long knownSize)
        {
            lock (_lock)
            {
                entries.Add(new CidEntry(cid, knownSize));
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

        public void Assign(AcDownloadStep downloadStep)
        {
            lock (_lock)
            {
                while (true)
                {
                    if (!entries.Any()) return;

                    var i = random.Next(0, entries.Count);
                    var entry = entries[i];

                    if (entry.CreatedUtc < (DateTime.UtcNow + TimeSpan.FromHours(18)))
                    {
                        entries.RemoveAt(i);
                    }
                    else
                    {
                        downloadStep.Cid = entry.Cid;
                        return;
                    }
                }
            }
        }

        public long? GetSizeKbsForCid(string cid)
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
        public CidEntry(string cid, long knownSize)
        {
            Cid = cid;
            KnownSize = knownSize;
        }

        public string Cid { get; }
        public string Encoded { get; set; } = string.Empty;
        public long KnownSize { get; }
        public DateTime CreatedUtc { get; } = DateTime.UtcNow;
    }
}
