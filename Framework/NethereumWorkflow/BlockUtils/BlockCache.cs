namespace NethereumWorkflow.BlockUtils
{
    public class BlockCache
    {
        public delegate void CacheClearedEvent();

        private const int MaxEntries = 1024 * 1024 * 5;
        private readonly Dictionary<ulong, BlockTimeEntry> entries = new Dictionary<ulong, BlockTimeEntry>();

        public event CacheClearedEvent? OnCacheCleared;

        public BlockTimeEntry Add(ulong number, DateTime dateTime)
        {
            return Add(new BlockTimeEntry(number, dateTime));
        }

        public BlockTimeEntry Add(BlockTimeEntry entry)
        {
            if (!entries.ContainsKey(entry.BlockNumber))
            {
                if (entries.Count > MaxEntries)
                {
                    entries.Clear();
                    var e = OnCacheCleared;
                    if (e != null) e();
                }
                entries.Add(entry.BlockNumber, entry);
            }

            return entries[entry.BlockNumber];
        }

        public BlockTimeEntry? Get(ulong number)
        {
            if (!entries.TryGetValue(number, out BlockTimeEntry? value)) return null;
            return value;
        }

        public int Size { get { return entries.Count; } }
    }
}
