using Logging;

namespace NethereumWorkflow.BlockUtils
{
    public class BlockTimeFinder
    {
        private readonly BlockCache cache;
        private readonly BlockchainBounds bounds;
        private readonly IWeb3Blocks web3;
        private readonly ILog log;

        public BlockTimeFinder(BlockCache cache, IWeb3Blocks web3, ILog log)
        {
            this.web3 = web3;
            this.log = log;

            this.cache = cache;
            bounds = new BlockchainBounds(cache, web3);
        }

        public ulong? GetHighestBlockNumberBefore(DateTime moment)
        {
            bounds.Initialize();
            if (moment <= bounds.Genesis.Utc) return null;
            if (moment >= bounds.Current.Utc) return bounds.Current.BlockNumber;

            return Log(() => Search(bounds.Genesis, bounds.Current, moment, HighestBeforeSelector));
        }

        public ulong? GetLowestBlockNumberAfter(DateTime moment)
        {
            bounds.Initialize();
            if (moment >= bounds.Current.Utc) return null;
            if (moment <= bounds.Genesis.Utc) return bounds.Genesis.BlockNumber;

            return Log(()=> Search(bounds.Genesis, bounds.Current, moment, LowestAfterSelector)); ;
        }

        private ulong Log(Func<ulong> operation)
        {
            var sw = Stopwatch.Begin(log, nameof(BlockTimeFinder));
            var result = operation();
            sw.End($"(Cache size: {cache.Size})");

            return result;
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
            ulong number = lower.BlockNumber + range / 2;
            return GetBlock(number);
        }

        private bool HighestBeforeSelector(DateTime target, BlockTimeEntry entry)
        {
            var next = GetBlock(entry.BlockNumber + 1);
            return
                entry.Utc <= target &&
                next.Utc > target;
        }

        private bool LowestAfterSelector(DateTime target, BlockTimeEntry entry)
        {
            var previous = GetBlock(entry.BlockNumber - 1);
            return
                entry.Utc >= target &&
                previous.Utc < target;
        }

        private BlockTimeEntry GetBlock(ulong number)
        {
            if (number < bounds.Genesis.BlockNumber) throw new Exception("Can't fetch block before genesis.");
            if (number > bounds.Current.BlockNumber) throw new Exception("Can't fetch block after current.");

            var dateTime = web3.GetTimestampForBlock(number);
            if (dateTime == null) throw new Exception("Failed to get dateTime for block that should exist.");
            return cache.Add(number, dateTime.Value);
        }
    }
}
