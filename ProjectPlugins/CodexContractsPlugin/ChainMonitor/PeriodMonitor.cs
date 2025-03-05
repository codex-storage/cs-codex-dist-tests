using Logging;
using Utils;

namespace CodexContractsPlugin.ChainMonitor
{
    public class PeriodMonitor
    {
        private readonly ILog log;
        private readonly ICodexContracts contracts;
        private readonly List<PeriodReport> reports = new List<PeriodReport>();
        private ulong? currentPeriod = null;

        public PeriodMonitor(ILog log, ICodexContracts contracts)
        {
            this.log = log;
            this.contracts = contracts;
        }

        public void Update(ulong blockNumber, DateTime eventUtc, IChainStateRequest[] requests)
        {
            var period = contracts.GetPeriodNumber(eventUtc);
            if (!currentPeriod.HasValue)
            {
                currentPeriod = period;
                return;
            }
            if (period == currentPeriod.Value) return;

            CreateReportForPeriod(blockNumber - 1, currentPeriod.Value, requests);
            currentPeriod = period;
        }

        public PeriodMonitorResult GetAndClearReports()
        {
            var result = reports.ToArray();
            reports.Clear();
            return new PeriodMonitorResult(result);
        }

        private void CreateReportForPeriod(ulong lastBlockInPeriod, ulong periodNumber, IChainStateRequest[] requests)
        {
            log.Log("Creating report for period " + periodNumber);

            ulong total = 0;
            ulong required = 0;
            var missed = new List<PeriodProofMissed>();
            foreach (var request in requests)
            {
                for (ulong slotIndex = 0; slotIndex < request.Request.Ask.Slots; slotIndex++)
                {
                    var state = contracts.GetProofState(request.Request, slotIndex, lastBlockInPeriod, periodNumber);

                    total++;
                    if (state.Required)
                    {
                        required++;
                        if (state.Missing)
                        {
                            var idx = Convert.ToInt32(slotIndex);
                            var host = request.Hosts.GetHost(idx);
                            missed.Add(new PeriodProofMissed(host, request, idx));
                        }
                    }
                }
            }
            reports.Add(new PeriodReport(periodNumber, total, required, missed.ToArray()));
        }
    }

    public class PeriodMonitorResult
    {
        public PeriodMonitorResult(PeriodReport[] reports)
        {
            Reports = reports;

            CalcStats();
        }

        public PeriodReport[] Reports { get; }

        public bool IsEmpty { get; private set; }
        public ulong PeriodLow { get; private set; }
        public ulong PeriodHigh { get; private set; }
        public float AverageNumSlots { get; private set; }
        public float AverageNumProofsRequired { get; private set; }

        private void CalcStats()
        {
            IsEmpty = Reports.Any(r => r.TotalProofsRequired > 0);
            PeriodLow = Reports.Min(r => r.PeriodNumber);
            PeriodHigh = Reports.Max(r => r.PeriodNumber);
            AverageNumSlots = Reports.Average(r => Convert.ToSingle(r.TotalNumSlots));
            AverageNumProofsRequired = Reports.Average(r => Convert.ToSingle(r.TotalProofsRequired));
        }
    }

    public class PeriodReport
    {
        public PeriodReport(ulong periodNumber, ulong totalNumSlots, ulong totalProofsRequired, PeriodProofMissed[] missedProofs)
        {
            PeriodNumber = periodNumber;
            TotalNumSlots = totalNumSlots;
            TotalProofsRequired = totalProofsRequired;
            MissedProofs = missedProofs;
        }

        public ulong PeriodNumber { get; }
        public ulong TotalNumSlots { get; }
        public ulong TotalProofsRequired { get; }
        public PeriodProofMissed[] MissedProofs { get; }
    }

    public class PeriodProofMissed
    {
        public PeriodProofMissed(EthAddress? host, IChainStateRequest request, int slotIndex)
        {
            Host = host;
            Request = request;
            SlotIndex = slotIndex;
        }

        public EthAddress? Host { get; }
        public IChainStateRequest Request { get; }
        public int SlotIndex { get; }
    }
}
