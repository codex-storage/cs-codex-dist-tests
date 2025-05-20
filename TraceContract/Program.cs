using System.Numerics;
using BlockchainUtils;
using CodexContractsPlugin;
using CodexContractsPlugin.ChainMonitor;
using CodexContractsPlugin.Marketplace;
using Core;
using GethPlugin;
using Logging;
using Nethereum.Hex.HexConvertors.Extensions;
using Utils;

namespace TraceContract
{
    public class Input
    {
        public string PurchaseId { get; } =
            // expired:
            //"a7fe97dc32216aba0cbe74b87beb3f919aa116090dd5e0d48085a1a6b0080e82";

            // started:
            "066df09a3a2e2587cfd577a0e96186c915b113d02b331b06e56f808494cff2b4";
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var p = new Program();
            p.Run();
        }

        private readonly ILog log = new ConsoleLog();
        private readonly Input input = new();
        private readonly Config config = new();
        private readonly Output output;

        public Program()
        {
            output = new(log);
        }

        private void Run()
        {
            try
            {
                TracePurchase();
            }
            catch (Exception exc)
            {
                log.Error(exc.ToString());
            }
        }

        private void TracePurchase()
        { 
            Log("Setting up...");
            var contracts = ConnectCodexContracts();
            
            var chainTracer = new ChainTracer(log, contracts, input, output);
            var requestTimeRange = chainTracer.TraceChainTimeline();

            Log("Done");
        }

        private ICodexContracts ConnectCodexContracts()
        {
            ProjectPlugin.Load<GethPlugin.GethPlugin>();
            ProjectPlugin.Load<CodexContractsPlugin.CodexContractsPlugin>();

            var entryPoint = new EntryPoint(log, new KubernetesWorkflow.Configuration(null, TimeSpan.FromMinutes(1.0), TimeSpan.FromSeconds(10.0), "_Unused!_"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            entryPoint.Announce();
            var ci = entryPoint.CreateInterface();

            var account = EthAccountGenerator.GenerateNew();
            var blockCache = new BlockCache();
            var geth = new CustomGethNode(log, blockCache, config.RpcEndpoint, config.GethPort, account.PrivateKey);

            var deployment = new CodexContractsDeployment(
                config: new CodexContractsPlugin.Marketplace.MarketplaceConfig(),
                marketplaceAddress: config.MarketplaceAddress,
                abi: config.Abi,
                tokenAddress: config.TokenAddress
            );
            return ci.WrapCodexContractsDeployment(geth, deployment);
        }

        private void Log(string msg)
        {
            log.Log(msg);
        }
    }

    public class ChainTracer
    {
        private readonly ILog log;
        private readonly ICodexContracts contracts;
        private readonly Input input;
        private readonly Output output;

        public ChainTracer(ILog log, ICodexContracts contracts, Input input, Output output)
        {
            this.log = log;
            this.contracts = contracts;
            this.input = input;
            this.output = output;
        }

        public TimeRange TraceChainTimeline()
        {
            var request = GetRequest();
            if (request == null) throw new Exception("Failed to find the purchase in the last week of transactions.");

            var utc = request.Block.Utc.AddMinutes(-1.0);
            var tracker = new ChainRequestTracker(output, input.PurchaseId);
            var ignoreLog = new NullLog();
            var chainState = new ChainState(ignoreLog, contracts, tracker, utc, false);

            while (!tracker.IsFinished)
            {
                utc += TimeSpan.FromHours(1.0);
                chainState.Update(utc);
            }

            var requestTimeline = new TimeRange(request.Block.Utc.AddMinutes(-1.0), tracker.FinishUtc.AddMinutes(1.0));

            // For this timeline, we log all the calls to reserve-slot.
            var events = contracts.GetEvents(requestTimeline);
            output.LogReserveSlotCalls(Filter(events.GetReserveSlotCalls()));

            output.WriteContractEvents();

            return requestTimeline;
        }

        private ReserveSlotFunction[] Filter(ReserveSlotFunction[] calls)
        {
            return calls.Where(c => IsThisRequest(c.RequestId)).ToArray();
        }

        private Request? GetRequest()
        {
            var request = FindRequest(LastHour());
            if (request == null) request = FindRequest(LastDay());
            if (request == null) request = FindRequest(LastWeek());
            return request;
        }

        private Request? FindRequest(TimeRange timeRange)
        {
            var events = contracts.GetEvents(timeRange);
            var requests = events.GetStorageRequests();

            foreach (var r in requests)
            {
                if (IsThisRequest(r.RequestId))
                {
                    return r;
                }
            }

            return null;
        }

        private bool IsThisRequest(byte[] requestId)
        {
            return requestId.ToHex().ToLowerInvariant() == input.PurchaseId.ToLowerInvariant();
        }

        private static TimeRange LastHour()
        {
            return new TimeRange(DateTime.UtcNow.AddHours(-1.0), DateTime.UtcNow);
        }

        private static TimeRange LastDay()
        {
            return new TimeRange(DateTime.UtcNow.AddDays(-1.0), DateTime.UtcNow);
        }

        private static TimeRange LastWeek()
        {
            return new TimeRange(DateTime.UtcNow.AddDays(-7.0), DateTime.UtcNow);
        }
    }

    public class ChainRequestTracker : IChainStateChangeHandler
    {
        private readonly string requestId;
        private readonly Output output;

        public ChainRequestTracker(Output output, string requestId)
        {
            this.requestId = requestId.ToLowerInvariant();
            this.output = output;
        }

        public bool IsFinished { get; private set; } = false;
        public DateTime FinishUtc { get; private set; } = DateTime.MinValue;

        public void OnError(string msg)
        {
        }

        public void OnNewRequest(RequestEvent requestEvent)
        {
            if (IsMyRequest(requestEvent)) output.LogRequestCreated(requestEvent);
        }

        public void OnProofSubmitted(BlockTimeEntry block, string id)
        {
        }

        public void OnRequestCancelled(RequestEvent requestEvent)
        {
            if (IsMyRequest(requestEvent))
            {
                IsFinished = true;
                FinishUtc = requestEvent.Block.Utc;
                output.LogRequestCancelled(requestEvent);
            }
        }

        public void OnRequestFailed(RequestEvent requestEvent)
        {
            if (IsMyRequest(requestEvent))
            {
                IsFinished = true;
                FinishUtc = requestEvent.Block.Utc;
                output.LogRequestFailed(requestEvent);
            }
        }

        public void OnRequestFinished(RequestEvent requestEvent)
        {
            if (IsMyRequest(requestEvent))
            {
                IsFinished = true;
                FinishUtc = requestEvent.Block.Utc;
                output.LogRequestFinished(requestEvent);
            }
        }

        public void OnRequestFulfilled(RequestEvent requestEvent)
        {
            if (IsMyRequest(requestEvent))
            {
                output.LogRequestStarted(requestEvent);
            }
        }

        public void OnSlotFilled(RequestEvent requestEvent, EthAddress host, BigInteger slotIndex)
        {
            if (IsMyRequest(requestEvent))
            {
                output.LogSlotFilled(requestEvent, host, slotIndex);
            }
        }

        public void OnSlotFreed(RequestEvent requestEvent, BigInteger slotIndex)
        {
            if (IsMyRequest(requestEvent))
            {
                output.LogSlotFreed(requestEvent, slotIndex);
            }
        }

        public void OnSlotReservationsFull(RequestEvent requestEvent, BigInteger slotIndex)
        {
            if (IsMyRequest(requestEvent))
            {
                output.LogSlotReservationsFull(requestEvent, slotIndex);
            }
        }

        private bool IsMyRequest(RequestEvent requestEvent)
        {
            return requestId == requestEvent.Request.Request.Id.ToLowerInvariant();
        }
    }

}
