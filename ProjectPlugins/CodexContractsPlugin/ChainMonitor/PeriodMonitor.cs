using Logging;
using Nethereum.Hex.HexConvertors.Extensions;
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
            ulong total = 0;
            var required = new List<PeriodProof>();
            var missed = new List<PeriodProof>();
            foreach (var request in requests)
            {
                for (ulong slotIndex = 0; slotIndex < request.Request.Ask.Slots; slotIndex++)
                {
                    var state = contracts.GetProofState(request.RequestId, slotIndex, lastBlockInPeriod, periodNumber);

                    total++;
                    if (state.Required)
                    {
                        var idx = Convert.ToInt32(slotIndex);
                        var host = request.Hosts.GetHost(idx);
                        var proof = new PeriodProof(host, request, idx);

                        required.Add(proof);
                        if (state.Missing)
                        {
                            missed.Add(proof);
                        }
                    }
                }
            }
            var report = new PeriodReport(periodNumber, total, required.ToArray(), missed.ToArray());
            log.Log($"Period report: {report}");
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
            IsEmpty = Reports.All(r => r.ProofsRequired.Length == 0);
            if (Reports.Length == 0) return;

            PeriodLow = Reports.Min(r => r.PeriodNumber);
            PeriodHigh = Reports.Max(r => r.PeriodNumber);
            AverageNumSlots = Reports.Average(r => Convert.ToSingle(r.TotalNumSlots));
            AverageNumProofsRequired = Reports.Average(r => Convert.ToSingle(r.ProofsRequired.Length));
        }
    }

    public class PeriodReport
    {
        public PeriodReport(ulong periodNumber, ulong totalNumSlots, PeriodProof[] proofsRequired, PeriodProof[] missedProofs)
        {
            PeriodNumber = periodNumber;
            TotalNumSlots = totalNumSlots;
            ProofsRequired = proofsRequired;
            MissedProofs = missedProofs;
        }

        public ulong PeriodNumber { get; }
        public ulong TotalNumSlots { get; }
        public PeriodProof[] ProofsRequired { get; }
        public PeriodProof[] MissedProofs { get; }

        public override string ToString()
        {
            var required = Describe(ProofsRequired);
            var missed = Describe(MissedProofs);
            return $"Period:{PeriodNumber}=[Slots:{TotalNumSlots},ProofsRequired:{required},ProofsMissed:{missed}]";
        }

        private string Describe(PeriodProof[] proofs)
        {
            if (proofs.Length == 0) return "None";
            return string.Join("+", proofs.Select(p => $"{p.FormatHost()} - {p.Request.RequestId.ToHex()} slot {p.SlotIndex}"));
        }
    }

    public class PeriodProof
    {
        public PeriodProof(EthAddress? host, IChainStateRequest request, int slotIndex)
        {
            Host = host;
            Request = request;
            SlotIndex = slotIndex;
        }

        public EthAddress? Host { get; }
        public IChainStateRequest Request { get; }
        public int SlotIndex { get; }

        public string FormatHost()
        {
            if (Host == null) return "Unknown host";
            return Host.Address;
        }
    }
}
