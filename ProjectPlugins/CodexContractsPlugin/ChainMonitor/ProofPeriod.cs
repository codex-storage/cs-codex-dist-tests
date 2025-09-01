using Utils;

namespace CodexContractsPlugin.ChainMonitor
{
    public class ProofPeriod
    {
        public ProofPeriod(ulong periodNumber, TimeRange timeRange, BlockInterval blockRange)
        {
            PeriodNumber = periodNumber;
            TimeRange = timeRange;
            BlockRange = blockRange;
        }

        public ulong PeriodNumber { get; }
        public TimeRange TimeRange { get; }
        public BlockInterval BlockRange { get; }

        public override string ToString()
        {
            return $"{{{PeriodNumber} - {TimeRange} {BlockRange}}}";
        }
    }
}
