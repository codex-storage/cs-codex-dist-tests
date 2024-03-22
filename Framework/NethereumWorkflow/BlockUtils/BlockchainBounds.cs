namespace NethereumWorkflow.BlockUtils
{
    public class BlockchainBounds
    {
        private readonly BlockCache cache;
        private readonly IWeb3Blocks web3;

        public BlockTimeEntry Genesis { get; private set; } = null!;
        public BlockTimeEntry Current { get; private set; } = null!;

        public BlockchainBounds(BlockCache cache, IWeb3Blocks web3)
        {
            this.cache = cache;
            this.web3 = web3;

            cache.OnCacheCleared += Initialize;
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

        private void LookForGenesisBlock()
        {
            if (Genesis != null)
            {
                cache.Add(Genesis);
                return;
            }

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

            var current = lower + range / 2;

            var blockTime = web3.GetTimestampForBlock(current);
            if (blockTime != null)
            {
                var newUpper = cache.Add(current, blockTime.Value);
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
            cache.Add(Current);
        }

        private void AddGenesisBlock(ulong number, DateTime dateTime)
        {
            AddGenesisBlock(new BlockTimeEntry(number, dateTime));
        }

        private void AddGenesisBlock(BlockTimeEntry entry)
        {
            Genesis = entry;
            cache.Add(Genesis);
        }
    }
}
