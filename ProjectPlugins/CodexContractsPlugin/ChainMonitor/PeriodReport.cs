using Logging;
using Utils;

namespace CodexContractsPlugin.ChainMonitor
{
    public class PeriodReport
    {
        public PeriodReport(ProofPeriod period, PeriodRequiredProof[] required, FunctionCallReport[] functionCalls)
        {
            Period = period;
            Required = required;
            FunctionCalls = functionCalls;
        }

        public ProofPeriod Period { get; }
        public PeriodRequiredProof[] Required { get; }
        public FunctionCallReport[] FunctionCalls { get; }

        public void Log(ILog log)
        {
            log.Log($"Period report: {Period}");
            foreach (var r in Required)
            {
                log.Log($"  Required: {r.Describe()}");
            }
            log.Log($" - Calls: {FunctionCalls.Length}");
            foreach (var f in FunctionCalls)
            {
                log.Log($"   - {f.Describe()}");
            }
        }
    }

    public class FunctionCallReport
    {
        public FunctionCallReport(DateTime utc, ulong blockNumber, string name, string payload)
        {
            Utc = utc;
            BlockNumber = blockNumber;
            Name = name;
            Payload = payload;
        }

        public DateTime Utc { get; }
        public ulong BlockNumber { get; }
        public string Name { get; }
        public string Payload { get; }

        public string Describe()
        {
            return $"[{Time.FormatTimestamp(Utc)}][{BlockNumber}] {Name} = \"{Payload}\"";
        }
    }
}
