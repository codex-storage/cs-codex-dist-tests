using CodexContractsPlugin.Marketplace;
using GethPlugin;
using Logging;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Model;
using Nethereum.RPC.Eth.DTOs;
using Newtonsoft.Json;
using System.Reflection;

namespace CodexContractsPlugin.ChainMonitor
{
    public class PeriodMonitor
    {
        private readonly ILog log;
        private readonly ICodexContracts contracts;
        private readonly IGethNode geth;
        private readonly List<PeriodReport> reports = new List<PeriodReport>();
        private CurrentPeriod? currentPeriod = null;

        public PeriodMonitor(ILog log, ICodexContracts contracts, IGethNode geth)
        {
            this.log = log;
            this.contracts = contracts;
            this.geth = geth;
        }

        public void Update(DateTime eventUtc, IChainStateRequest[] requests)
        {
            var periodNumber = contracts.GetPeriodNumber(eventUtc);
            if (currentPeriod == null)
            {
                currentPeriod = CreateCurrentPeriod(periodNumber, requests);
                return;
            }
            if (periodNumber == currentPeriod.PeriodNumber) return;

            CreateReportForPeriod(currentPeriod, requests);
            currentPeriod = CreateCurrentPeriod(periodNumber, requests);
        }

        public PeriodMonitorResult GetAndClearReports()
        {
            var result = reports.ToArray();
            reports.Clear();
            return new PeriodMonitorResult(result);
        }

        private CurrentPeriod CreateCurrentPeriod(ulong periodNumber, IChainStateRequest[] requests)
        {
            var result = new CurrentPeriod(periodNumber);
            ForEachActiveSlot(requests, (request, slotIndex) =>
            {
                if (contracts.IsProofRequired(request.RequestId, slotIndex))
                {
                    var idx = Convert.ToInt32(slotIndex);
                    var host = request.Hosts.GetHost(idx);
                    if (host != null)
                    {
                        result.RequiredProofs.Add(new PeriodRequiredProof(host, request, idx));
                    }
                }
            });

            return result;
        }

        private void CreateReportForPeriod(CurrentPeriod currentPeriod, IChainStateRequest[] requests)
        {
            // Fetch function calls during period. Format report.
            var timeRange = contracts.GetPeriodTimeRange(currentPeriod.PeriodNumber);
            var blockRange = geth.ConvertTimeRangeToBlockRange(timeRange);

            // MarkProofAsMissingFunction
            // SubmitProofFunction
            // FreeSlot1Function
            // FreeSlotFunction

            var callReports = new List<FunctionCallReport>();
            geth.IterateTransactions(blockRange, (t, blkI, blkUtc) =>
            {
                CreateFunctionCallReport<MarkProofAsMissingFunction>(callReports, t, blkUtc);
                CreateFunctionCallReport<SubmitProofFunction>(callReports, t, blkUtc);
                CreateFunctionCallReport<FreeSlot1Function>(callReports, t, blkUtc);
                CreateFunctionCallReport<FreeSlotFunction>(callReports, t, blkUtc);
            });

            var report = new PeriodReport(
                new ProofPeriod(
                    currentPeriod.PeriodNumber,
                    timeRange.From,
                    timeRange.To),
                currentPeriod.RequiredProofs.ToArray(),
                callReports.ToArray());

            report.Log(log);
            reports.Add(report);
        }

        private void CreateFunctionCallReport<TFunc>(List<FunctionCallReport> reports, Transaction t, DateTime blockUtc) where TFunc : FunctionMessage, new()
        {
            if (t.IsTransactionForFunctionMessage<TFunc>())
            {
                var func = t.DecodeTransactionToFunctionMessage<TFunc>();

                reports.Add(new FunctionCallReport(blockUtc, typeof(TFunc).Name, JsonConvert.SerializeObject(func)));
            }
        }

        private void ForEachActiveSlot(IChainStateRequest[] requests, Action<IChainStateRequest, ulong> action)
        {
            foreach (var request in requests)
            {
                for (ulong slotIndex = 0; slotIndex < request.Request.Ask.Slots; slotIndex++)
                {
                    action(request, slotIndex);
                }
            }
        }
    }

    public class PeriodMonitorResult
    {
        public PeriodMonitorResult(PeriodReport[] reports)
        {
            Reports = reports;
        }

        public PeriodReport[] Reports { get; }
    }

    public class CurrentPeriod
    {
        public CurrentPeriod(ulong periodNumber)
        {
            PeriodNumber = periodNumber;
        }

        public ulong PeriodNumber { get; }
        public List<PeriodRequiredProof> RequiredProofs { get; } = new List<PeriodRequiredProof>();
    }
}
