using Utils;

namespace CodexContractsPlugin.ChainMonitor
{
    public class ProofPeriod
    {
        public ProofPeriod(ulong periodNumber, DateTime startUtc, DateTime endUtc)
        {
            PeriodNumber = periodNumber;
            StartUtc = startUtc;
            EndUtc = endUtc;
        }

        public ulong PeriodNumber { get; }
        public DateTime StartUtc { get; }
        public DateTime EndUtc { get; }

        public override string ToString()
        {
            return $"{PeriodNumber} - {Time.FormatTimestamp(StartUtc)} -> {Time.FormatTimestamp(EndUtc)}";
        }
    }
}
