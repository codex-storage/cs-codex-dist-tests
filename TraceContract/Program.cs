using BlockchainUtils;
using CodexContractsPlugin;
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
        public string PurchaseId { get; } = "a7fe97dc32216aba0cbe74b87beb3f919aa116090dd5e0d48085a1a6b0080e82";
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
        private readonly Output output = new();
        private readonly Config config = new();

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
            chainTracer.TraceChainTimeline();

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

        public void TraceChainTimeline()
        {
            var request = GetRequest();
            if (request == null) throw new Exception("Failed to find the purchase in the last week of transactions.");
            output.LogRequest(request);

            var requestTimeRange = new TimeRange(request.Block.Utc.AddMinutes(-1.0), DateTime.UtcNow);
            var events = contracts.GetEvents(requestTimeRange);

            // Log calls to reserve slot for request
            var calls = Filter(events.GetReserveSlotCalls());
            output.LogEventOrCall(calls);

            // log all events.
            var fulls = events.GetSlotReservationsFullEvents();
            output.LogEventOrCall(Filter(fulls));

        }

        private T[] Filter<T>(T[] calls) where T : IHasRequestId
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

        private TimeRange LastHour()
        {
            return new TimeRange(DateTime.UtcNow.AddHours(-1.0), DateTime.UtcNow);
        }

        private TimeRange LastDay()
        {
            return new TimeRange(DateTime.UtcNow.AddDays(-1.0), DateTime.UtcNow);
        }

        private TimeRange LastWeek()
        {
            return new TimeRange(DateTime.UtcNow.AddDays(-7.0), DateTime.UtcNow);
        }

        private void Log(string msg)
        {
            log.Log(msg);
        }
    }
}
