namespace BlockchainUtils
{
    public class BlockTimeEntry
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
}
