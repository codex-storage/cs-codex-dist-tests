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
        }

        private const ulong FetchRange = 6;
        private const int MaxEntries = 1024;
        private readonly Web3 web3;
        private static readonly Dictionary<ulong, BlockTimeEntry> entries = new Dictionary<ulong, BlockTimeEntry>();
        
        public BlockTimeFinder(Web3 web3)
        {
            this.web3 = web3;
        }

        public ulong GetHighestBlockNumberBefore(DateTime moment)
        {
            AssertMomentIsInPast(moment);
            Initialize();

            var closestBefore = FindClosestBeforeEntry(moment);
            var closestAfter = FindClosestAfterEntry(moment);
          
            if (closestBefore.Utc < moment &&
                closestAfter.Utc > moment &&
                closestBefore.BlockNumber + 1 == closestAfter.BlockNumber)
            {
                return closestBefore.BlockNumber;
            }

            FetchBlocksAround(moment);
            return GetHighestBlockNumberBefore(moment);
        }

        public ulong GetLowestBlockNumberAfter(DateTime moment)
        {
            AssertMomentIsInPast(moment);
            Initialize();

            var closestBefore = FindClosestBeforeEntry(moment);
            var closestAfter = FindClosestAfterEntry(moment);

            if (closestBefore.Utc < moment &&
                closestAfter.Utc > moment &&
                closestBefore.BlockNumber + 1 == closestAfter.BlockNumber)
            {
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
            var latest = entries[max];
            var timeDifference = latest.Utc - moment;
            double secondsDifference = Math.Abs(timeDifference.TotalSeconds);
            double secondsPerBlock = timePerBlock.TotalSeconds;

            double numberOfBlocksDifference = secondsDifference / secondsPerBlock;
            var blockDifference = Convert.ToUInt64(numberOfBlocksDifference);

            var fetchStart = (max - blockDifference) - (FetchRange / 2);
            for (ulong i = 0; i < FetchRange; i++)
            {
                AddBlockNumber(fetchStart + i);
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
                if (newBlock.BlockNumber == latest.BlockNumber)
                {
                    maxRetry--;
                    if (maxRetry == 0) throw new Exception("Unable to fetch recent block after 10x tries.");
                    Thread.Sleep(timePerBlock);
                }
            }
        }

        private BlockTimeEntry AddBlockNumber(decimal blockNumber)
        {
            return AddBlockNumber(Convert.ToUInt64(blockNumber));
        }

        private BlockTimeEntry AddBlockNumber(ulong blockNumber)
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
            var entry = new BlockTimeEntry(blockNumber, time);
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

        private BlockTimeEntry AddCurrentBlock()
        {
            var number = Time.Wait(web3.Eth.Blocks.GetBlockNumber.SendRequestAsync());
            var blockNumber = number.ToDecimal();
            return AddBlockNumber(blockNumber);
        }

        private DateTime GetTimestampFromBlock(ulong blockNumber)
        {
            var block = Time.Wait(web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new BlockParameter(blockNumber)));
            return DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(block.Timestamp.ToDecimal())).UtcDateTime;
        }

        private BlockTimeEntry FindClosestBeforeEntry(DateTime moment)
        {
            var result = entries.Values.First();
            var highestTime = result.Utc;

            foreach (var entry in entries.Values)
            {
                if (entry.Utc > highestTime && entry.Utc < moment)
                {
                    highestTime = entry.Utc;
                    result = entry;
                }
            }
            return result;
        }

        private BlockTimeEntry FindClosestAfterEntry(DateTime moment)
        {
            var result = entries.Values.First();
            var lowestTime = result.Utc;

            foreach (var entry in entries.Values)
            {
                if (entry.Utc < lowestTime && entry.Utc > moment)
                {
                    lowestTime = entry.Utc;
                    result = entry;
                }
            }
            return result;
        }
    }
}
