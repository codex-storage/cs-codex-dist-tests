using Logging;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Model;
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

        public void Update(DateTime eventUtc, IChainStateRequest[] requests)
        {
            var period = contracts.GetPeriodNumber(eventUtc);
            if (!currentPeriod.HasValue)
            {
                currentPeriod = period;
                return;
            }
            if (period == currentPeriod.Value) return;

            CreateReportForPeriod(currentPeriod.Value, requests);
            currentPeriod = period;
        }

        public PeriodMonitorResult GetAndClearReports()
        {
            var result = reports.ToArray();
            reports.Clear();
            return new PeriodMonitorResult(result);
        }

        private void CreateReportForPeriod(ulong periodNumber, IChainStateRequest[] requests)
        {
            ulong total = 0;
            var periodProofs = new List<PeriodProof>();
            foreach (var request in requests)
            {
                for (ulong slotIndex = 0; slotIndex < request.Request.Ask.Slots; slotIndex++)
                {
                    var state = contracts.GetProofState(request.RequestId, slotIndex, periodNumber);

                    total++;
                    var idx = Convert.ToInt32(slotIndex);
                    var host = request.Hosts.GetHost(idx);
                    var proof = new PeriodProof(host, request, idx, state);
                    periodProofs.Add(proof);
                }
            }
            var report = new PeriodReport(periodNumber, total, periodProofs.ToArray());
            report.Log(log);
            reports.Add(report);
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
            IsEmpty = Reports.All(r => r.PeriodProofs.Length == 0);
            if (Reports.Length == 0) return;

            PeriodLow = Reports.Min(r => r.PeriodNumber);
            PeriodHigh = Reports.Max(r => r.PeriodNumber);
            AverageNumSlots = Reports.Average(r => Convert.ToSingle(r.TotalNumSlots));
            AverageNumProofsRequired = Reports.Average(r => Convert.ToSingle(r.PeriodProofs.Count(p => p.State != ProofState.NotRequired)));
        }
    }

    public class PeriodReport
    {
        public PeriodReport(ulong periodNumber, ulong totalNumSlots, PeriodProof[] periodProofs)
        {
            PeriodNumber = periodNumber;
            TotalNumSlots = totalNumSlots;
            PeriodProofs = periodProofs;
        }

        public ulong PeriodNumber { get; }
        public ulong TotalNumSlots { get; }
        public PeriodProof[] PeriodProofs { get; }

        public PeriodProof[] GetMissedProofs()
        {
            return PeriodProofs.Where(p => p.State == ProofState.MissedAndMarked || p.State == ProofState.MissedNotMarked).ToArray();
        }

        public void Log(ILog log)
        {
            log.Log($"Period report: {PeriodNumber}");
            log.Log($"   - Slots: {TotalNumSlots}");
            foreach (var p in PeriodProofs)
            {
                log.Log($"   - {p.Describe()}");
            }
        }

        private void Log(ILog log, PeriodProof[] proofs)
        {
            if (proofs.Length == 0) return;
            foreach (var p in proofs)
            {
            }
        }
    }

    public class PeriodProof
    {
        public PeriodProof(EthAddress? host, IChainStateRequest request, int slotIndex, ProofState state)
        {
            Host = host;
            Request = request;
            SlotIndex = slotIndex;
            State = state;
        }

        public EthAddress? Host { get; }
        public IChainStateRequest Request { get; }
        public int SlotIndex { get; }
        public ProofState State { get; }

        public string FormatHost()
        {
            if (Host == null) return "Unknown host";
            return Host.Address;
        }

        public string Describe()
        {
            return $"{FormatHost()} - {Request.RequestId.ToHex()} slotIndex:{SlotIndex} => {State}";
        }
    }
}
