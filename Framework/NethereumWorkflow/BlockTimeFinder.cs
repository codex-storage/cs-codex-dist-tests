using Logging;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Org.BouncyCastle.Asn1.X509;
using Utils;

namespace NethereumWorkflow
{
    public partial class BlockTimeFinder
    {
        private const ulong FetchRange = 6;
        private const int MaxEntries = 1024;
        private static readonly Dictionary<ulong, BlockTimeEntry> entries = new Dictionary<ulong, BlockTimeEntry>();
        private readonly Web3 web3;
        private readonly ILog log;
        
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

            if (closestBefore != null &&
                closestAfter != null &&
                closestBefore.Utc < moment &&
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

            if (closestBefore != null &&
                closestAfter != null &&
                closestBefore.Utc < moment &&
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
            var timePerBlock = EstimateTimePerBlock();
            EnsureRecentBlockIfNecessary(moment, timePerBlock);

            var max = entries.Keys.Max();
            var blockDifference = CalculateBlockDifference(moment, timePerBlock, max);

            FetchUp(max, blockDifference);
            FetchDown(max, blockDifference);
        }

        private void FetchDown(ulong max, ulong blockDifference)
        {
            var target = max - blockDifference - 1;
            var fetchDown = FetchRange;
            while (fetchDown > 0)
            {
                if (!entries.ContainsKey(target))
                {
                    var newBlock = AddBlockNumber(target);
                    if (newBlock == null) return;
                    fetchDown--;
                }
                target--;
                if (target <= 0) return;
            }
        }

        private void FetchUp(ulong max, ulong blockDifference)
        {
            var target = max - blockDifference;
            var fetchUp = FetchRange;
            while (fetchUp > 0)
            {
                if (!entries.ContainsKey(target))
                {
                    var newBlock = AddBlockNumber(target);
                    if (newBlock == null) return;
                    fetchUp--;
                }
                target++;
                if (target >= max) return;
            }
        }

        private ulong CalculateBlockDifference(DateTime moment, TimeSpan timePerBlock, ulong max)
        {
            var latest = entries[max];
            var timeDifference = latest.Utc - moment;
            double secondsDifference = Math.Abs(timeDifference.TotalSeconds);
            double secondsPerBlock = timePerBlock.TotalSeconds;

            double numberOfBlocksDifference = secondsDifference / secondsPerBlock;
            var blockDifference = Convert.ToUInt64(numberOfBlocksDifference);
            if (blockDifference < 1) blockDifference = 1;
            return blockDifference;
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
                max = entries.Keys.Max();
                latest = entries[max];
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
            var clippedMin = Math.Max(max - 100, min);
            var minTime = entries[min].Utc;
            var clippedMinBlock = AddBlockNumber(clippedMin);
            if (clippedMinBlock != null) minTime = clippedMinBlock.Utc;

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
