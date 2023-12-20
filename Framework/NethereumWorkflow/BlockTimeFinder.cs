using Logging;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Utils;

namespace NethereumWorkflow
{
    public class BlockTimeFinder
    {
        private class BlockTimeEntry
        {
            public BlockTimeEntry(ulong blockNumber, DateTime utc)
            {
                BlockNumber = blockNumber;
                Utc = utc;
            }

            public ulong BlockNumber { get; }
            public DateTime Utc { get; }

            public override string ToString()
            {
                return $"[{BlockNumber}] @ {Utc.ToString("o")}";
            }
        }

        private const ulong FetchRange = 6;
        private const int MaxEntries = 1024;
        private readonly Web3 web3;
        private readonly ILog log;
        private static readonly Dictionary<ulong, BlockTimeEntry> entries = new Dictionary<ulong, BlockTimeEntry>();
        
        public BlockTimeFinder(Web3 web3, ILog log)
        {
            this.web3 = web3;
            this.log = log;
        }

        public ulong GetHighestBlockNumberBefore(DateTime moment)
        {
            log.Log("Looking for highest block before " + moment.ToString("o"));
            AssertMomentIsInPast(moment);
            Initialize();

            var closestBefore = FindClosestBeforeEntry(moment);
            var closestAfter = FindClosestAfterEntry(moment);

            if (closestBefore == null || closestAfter == null)
            {
                FetchBlocksAround(moment);
                return GetHighestBlockNumberBefore(moment);
            }

            log.Log("Closest before: " + closestBefore);
            log.Log("Closest after: " + closestAfter);

            if (closestBefore.Utc < moment &&
                closestAfter.Utc > moment &&
                closestBefore.BlockNumber + 1 == closestAfter.BlockNumber)
            {
                log.Log("Found highest-Before: " + closestBefore);
                return closestBefore.BlockNumber;
            }

            FetchBlocksAround(moment);
            return GetHighestBlockNumberBefore(moment);
        }

        public ulong GetLowestBlockNumberAfter(DateTime moment)
        {
            log.Log("Looking for lowest block after " + moment.ToString("o"));
            AssertMomentIsInPast(moment);
            Initialize();

            var closestBefore = FindClosestBeforeEntry(moment);
            var closestAfter = FindClosestAfterEntry(moment);

            if (closestBefore == null || closestAfter == null)
            {
                FetchBlocksAround(moment);
                return GetLowestBlockNumberAfter(moment);
            }

            log.Log("Closest before: " + closestBefore);
            log.Log("Closest after: " + closestAfter);

            if (closestBefore.Utc < moment &&
                closestAfter.Utc > moment &&
                closestBefore.BlockNumber + 1 == closestAfter.BlockNumber)
            {
                log.Log("Found lowest-after: " + closestAfter);
                return closestAfter.BlockNumber;
            }

            FetchBlocksAround(moment);
            return GetLowestBlockNumberAfter(moment);
        }

        private void FetchBlocksAround(DateTime moment)
        {
            log.Log("Fetching...");

            var timePerBlock = EstimateTimePerBlock();
            EnsureRecentBlockIfNecessary(moment, timePerBlock);

            var max = entries.Keys.Max();
            var latest = entries[max];
            var timeDifference = latest.Utc - moment;
            double secondsDifference = Math.Abs(timeDifference.TotalSeconds);
            double secondsPerBlock = timePerBlock.TotalSeconds;

            double numberOfBlocksDifference = secondsDifference / secondsPerBlock;
            var blockDifference = Convert.ToUInt64(numberOfBlocksDifference);
            if (blockDifference < 1) blockDifference = 1;

            var fetchUp = FetchRange;
            var fetchDown = FetchRange;
            var target = max - blockDifference;
            log.Log("up - target: " + target);
            while (fetchUp > 0)
            {
                if (!entries.ContainsKey(target))
                {
                    var newBlock = AddBlockNumber(target);
                    if (newBlock != null) fetchUp--;
                    else fetchUp = 0;
                }
                target++;
                //if (target >= max) fetchUp = 0;
            }

            target = max - blockDifference - 1;
            log.Log("down - target: " + target);
            while (fetchDown > 0)
            {
                if (!entries.ContainsKey(target))
                {
                    var newBlock = AddBlockNumber(target);
                    if (newBlock != null) fetchDown--;
                    else fetchDown = 0;
                }
                target--;
                //if (target <= 0) fetchDown = 0;
            }
        }

        private void EnsureRecentBlockIfNecessary(DateTime moment, TimeSpan timePerBlock)
        {
            var max = entries.Keys.Max();
            var latest = entries[max];
            var maxRetry = 10;
            while (moment > latest.Utc)
            {
                var newBlock = AddCurrentBlock();
                if (newBlock == null || newBlock.BlockNumber == latest.BlockNumber)
                {
                    maxRetry--;
                    if (maxRetry == 0) throw new Exception("Unable to fetch recent block after 10x tries.");
                    Thread.Sleep(timePerBlock);
                }
            }
        }

        private BlockTimeEntry? AddBlockNumber(decimal blockNumber)
        {
            return AddBlockNumber(Convert.ToUInt64(blockNumber));
        }

        private BlockTimeEntry? AddBlockNumber(ulong blockNumber)
        {
            if (entries.ContainsKey(blockNumber))
            {
                return entries[blockNumber];
            }

            if (entries.Count > MaxEntries)
            {
                entries.Clear();
                Initialize();
            }

            var time = GetTimestampFromBlock(blockNumber);
            if (time == null) return null;
            var entry = new BlockTimeEntry(blockNumber, time.Value);
            log.Log("Found block " + entry.BlockNumber + " at " + entry.Utc.ToString("o"));
            entries.Add(blockNumber, entry);
            return entry;
        }

        private TimeSpan EstimateTimePerBlock()
        {
            var min = entries.Keys.Min();
            var max = entries.Keys.Max();
            var minTime = entries[min].Utc;
            var maxTime = entries[max].Utc;
            var elapsedTime = maxTime - minTime;

            double elapsedSeconds = elapsedTime.TotalSeconds;
            double numberOfBlocks = max - min;
            double secondsPerBlock = elapsedSeconds / numberOfBlocks;

            return TimeSpan.FromSeconds(secondsPerBlock);
        }

        private void Initialize()
        {
            if (!entries.Any())
            {
                AddCurrentBlock();
                AddBlockNumber(entries.Single().Key - 1);
            }
        }

        private static void AssertMomentIsInPast(DateTime moment)
        {
            if (moment > DateTime.UtcNow) throw new Exception("Moment must be UTC and must be in the past.");
        }

        private BlockTimeEntry? AddCurrentBlock()
        {
            var number = Time.Wait(web3.Eth.Blocks.GetBlockNumber.SendRequestAsync());
            var blockNumber = number.ToDecimal();
            return AddBlockNumber(blockNumber);
        }

        private DateTime? GetTimestampFromBlock(ulong blockNumber)
        {
            var block = Time.Wait(web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new BlockParameter(blockNumber)));
            if (block == null) return null;
            return DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(block.Timestamp.ToDecimal())).UtcDateTime;
        }

        private BlockTimeEntry? FindClosestBeforeEntry(DateTime moment)
        {
            BlockTimeEntry? result = null;
            foreach (var entry in entries.Values)
            {
                if (result == null)
                {
                    if (entry.Utc < moment) result = entry;
                }
                else
                {
                    if (entry.Utc > result.Utc && entry.Utc < moment) result = entry;
                }
            }
            return result;
        }

        private BlockTimeEntry? FindClosestAfterEntry(DateTime moment)
        {
            BlockTimeEntry? result = null;
            foreach (var entry in entries.Values)
            {
                if (result == null)
                {
                    if (entry.Utc > moment) result = entry;
                }
                else
                {
                    if (entry.Utc < result.Utc && entry.Utc > moment) result = entry;
                }
            }
            return result;
        }
    }
}
