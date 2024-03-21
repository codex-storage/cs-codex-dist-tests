using Logging;

namespace NethereumWorkflow
{
    public class BlockTimeFinder
    {
        private readonly BlockCache cache;
        private readonly IWeb3Blocks web3;
        private readonly ILog log;
        
        public BlockTimeFinder(IWeb3Blocks web3, ILog log)
        {
            this.web3 = web3;
            this.log = log;

            cache = new BlockCache(web3);
        }

        public ulong? GetHighestBlockNumberBefore(DateTime moment)
        {
            cache.Initialize();
            if (moment <= cache.Genesis.Utc) return null;
            if (moment >= cache.Current.Utc) return cache.Current.BlockNumber;

            return Search(cache.Genesis, cache.Current, moment, HighestBeforeSelector);
        }

        public ulong? GetLowestBlockNumberAfter(DateTime moment)
        {
            cache.Initialize();
            if (moment >= cache.Current.Utc) return null;
            if (moment <= cache.Genesis.Utc) return cache.Genesis.BlockNumber;

            return Search(cache.Genesis, cache.Current, moment, LowestAfterSelector);
        }

        private ulong Search(BlockTimeEntry lower, BlockTimeEntry upper, DateTime target, Func<DateTime, BlockTimeEntry, bool> isWhatIwant)
        {
            var middle = GetMiddle(lower, upper);
            if (middle.BlockNumber == lower.BlockNumber)
            {
                if (isWhatIwant(target, upper)) return upper.BlockNumber;
            }

            if (isWhatIwant(target, middle))
            {
                return middle.BlockNumber;
            }

            if (middle.Utc > target)
            {
                return Search(lower, middle, target, isWhatIwant);
            }
            else
            {
                return Search(middle, upper, target, isWhatIwant);
            }
        }

        private BlockTimeEntry GetMiddle(BlockTimeEntry lower, BlockTimeEntry upper)
        {
            ulong range = upper.BlockNumber - lower.BlockNumber;
            ulong number = lower.BlockNumber + (range / 2);
            return GetBlock(number);
        }

        private bool HighestBeforeSelector(DateTime target, BlockTimeEntry entry)
        {
            var next = GetBlock(entry.BlockNumber + 1);
            return
                entry.Utc < target &&
                next.Utc > target;
        }

        private bool LowestAfterSelector(DateTime target, BlockTimeEntry entry)
        {
            var previous = GetBlock(entry.BlockNumber - 1);
            return
                entry.Utc > target &&
                previous.Utc < target;
        }

        private BlockTimeEntry GetBlock(ulong number)
        {
            if (number < cache.Genesis.BlockNumber) throw new Exception("Can't fetch block before genesis.");
            if (number > cache.Current.BlockNumber) throw new Exception("Can't fetch block after current.");

            var dateTime = web3.GetTimestampForBlock(number);
            if (dateTime == null) throw new Exception("Failed to get dateTime for block that should exist.");
            return cache.Add(number, dateTime.Value);
        }
    }

    public class BlockCache
    {
        private const int MaxEntries = 1024;
        private readonly Dictionary<ulong, BlockTimeEntry> entries = new Dictionary<ulong, BlockTimeEntry>();
        private readonly IWeb3Blocks web3;

        public BlockTimeEntry Genesis { get; private set; } = null!;
        public BlockTimeEntry Current { get; private set; } = null!;

        public BlockCache(IWeb3Blocks web3)
        {
            this.web3 = web3;
        }

        public void Initialize()
        {
            AddCurrentBlock();
            LookForGenesisBlock();

            if (Current.BlockNumber == Genesis.BlockNumber)
            {
                throw new Exception("Unsupported condition: Current block is genesis block.");
            }
        }

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
                    Initialize();
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

        private void LookForGenesisBlock()
        {
            if (Genesis != null) return;

            var blockTime = web3.GetTimestampForBlock(0);
            if (blockTime != null)
            {
                AddGenesisBlock(0, blockTime.Value);
                return;
            }

            LookForGenesisBlock(0, Current);
        }

        private void LookForGenesisBlock(ulong lower, BlockTimeEntry upper)
        {
            if (Genesis != null) return;

            var range = upper.BlockNumber - lower;
            if (range == 1)
            {
                var lowTime = web3.GetTimestampForBlock(lower);
                if (lowTime != null)
                {
                    AddGenesisBlock(lower, lowTime.Value);
                }
                else
                {
                    AddGenesisBlock(upper);
                }
                return;
            }

            var current = lower + (range / 2);

            var blockTime = web3.GetTimestampForBlock(current);
            if (blockTime != null)
            {
                var newUpper = Add(current, blockTime.Value);
                LookForGenesisBlock(lower, newUpper);
            }
            else
            {
                LookForGenesisBlock(current, upper);
            }
        }

        private void AddCurrentBlock()
        {
            var currentBlockNumber = web3.GetCurrentBlockNumber();
            var blockTime = web3.GetTimestampForBlock(currentBlockNumber);
            if (blockTime == null) throw new Exception("Unable to get dateTime for current block.");
            AddCurrentBlock(currentBlockNumber, blockTime.Value);
        }

        private void AddCurrentBlock(ulong currentBlockNumber, DateTime dateTime)
        {
            Current = new BlockTimeEntry(currentBlockNumber, dateTime);
            Add(Current);
        }

        private void AddGenesisBlock(ulong number, DateTime dateTime)
        {
            AddGenesisBlock(new BlockTimeEntry(number, dateTime));
        }

        private void AddGenesisBlock(BlockTimeEntry entry)
        {
            Genesis = entry;
            Add(Genesis);
        }
    }
}
