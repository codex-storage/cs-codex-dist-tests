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
    public interface IPeriodMonitorEventHandler
    {
        void OnPeriodReport(PeriodReport report);
    }

    public class PeriodMonitor
    {
        private readonly ILog log;
        private readonly ICodexContracts contracts;
        private readonly IGethNode geth;
        private readonly IPeriodMonitorEventHandler eventHandler;
        private readonly List<PeriodReport> reports = new List<PeriodReport>();
        private CurrentPeriod? currentPeriod = null;

        public PeriodMonitor(ILog log, ICodexContracts contracts, IGethNode geth, IPeriodMonitorEventHandler eventHandler)
        {
            this.log = log;
            this.contracts = contracts;
            this.geth = geth;
            this.eventHandler = eventHandler;
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
                    var slotId = contracts.GetSlotId(request.RequestId, slotIndex);
                    if (host != null)
                    {
                        result.RequiredProofs.Add(new PeriodRequiredProof(host, request, idx, slotId));
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

            var callReports = new List<FunctionCallReport>();
            geth.IterateTransactions(blockRange, (t, blkI, blkUtc) =>
            {
                var reporter = new CallReporter(callReports, t, blkUtc, blkI);
                reporter.Run();

            });

            var report = new PeriodReport(
                new ProofPeriod(currentPeriod.PeriodNumber, timeRange, blockRange),
                currentPeriod.RequiredProofs.ToArray(),
                callReports.ToArray());

            report.Log(log);
            reports.Add(report);

            eventHandler.OnPeriodReport(report);
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

    public class DoNothingPeriodMonitorEventHandler : IPeriodMonitorEventHandler
    {
        public void OnPeriodReport(PeriodReport report)
        {
        }
    }

    public class CallReporter
    {
        private readonly List<FunctionCallReport> reports;
        private readonly Transaction t;
        private readonly DateTime blockUtc;
        private readonly ulong blockNumber;

        public CallReporter(List<FunctionCallReport> reports, Transaction t, DateTime blockUtc, ulong blockNumber)
        {
            this.reports = reports;
            this.t = t;
            this.blockUtc = blockUtc;
            this.blockNumber = blockNumber;
        }

        public void Run()
        {
            CreateFunctionCallReport<CanMarkProofAsMissingFunction>();
            CreateFunctionCallReport<CanReserveSlotFunction>();
            CreateFunctionCallReport<ConfigurationFunction>();
            CreateFunctionCallReport<CurrentCollateralFunction>();
            CreateFunctionCallReport<FillSlotFunction>();
            CreateFunctionCallReport<FreeSlot1Function>();
            CreateFunctionCallReport<FreeSlotFunction>();
            CreateFunctionCallReport<GetActiveSlotFunction>();
            CreateFunctionCallReport<GetChallengeFunction>();
            CreateFunctionCallReport<GetHostFunction>();
            CreateFunctionCallReport<GetPointerFunction>();
            CreateFunctionCallReport<GetRequestFunction>();
            CreateFunctionCallReport<IsProofRequiredFunction>();
            CreateFunctionCallReport<MarkProofAsMissingFunction>();
            CreateFunctionCallReport<MissingProofsFunction>();
            CreateFunctionCallReport<MyRequestsFunction>();
            CreateFunctionCallReport<MySlotsFunction>();
            CreateFunctionCallReport<RequestEndFunction>();
            CreateFunctionCallReport<RequestExpiryFunction>();
            CreateFunctionCallReport<RequestStateFunction>();
            CreateFunctionCallReport<RequestStorageFunction>();
            CreateFunctionCallReport<ReserveSlotFunction>();
            CreateFunctionCallReport<SlotProbabilityFunction>();
            CreateFunctionCallReport<SlotStateFunction>();
            CreateFunctionCallReport<SubmitProofFunction>();
            CreateFunctionCallReport<TokenFunction>();
            CreateFunctionCallReport<WillProofBeRequiredFunction>();
            CreateFunctionCallReport<WithdrawFundsFunction>();
            CreateFunctionCallReport<WithdrawFunds1Function>();
        }

        private void CreateFunctionCallReport<TFunc>() where TFunc : FunctionMessage, new()
        {
            if (t.IsTransactionForFunctionMessage<TFunc>())
            {
                var func = t.DecodeTransactionToFunctionMessage<TFunc>();

                reports.Add(new FunctionCallReport(blockUtc, blockNumber, typeof(TFunc).Name, JsonConvert.SerializeObject(func)));
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
