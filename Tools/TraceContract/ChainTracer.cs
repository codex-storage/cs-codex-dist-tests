using CodexContractsPlugin;
using CodexContractsPlugin.ChainMonitor;
using CodexContractsPlugin.Marketplace;
using Logging;
using Nethereum.Hex.HexConvertors.Extensions;
using Utils;

namespace TraceContract
{
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
            log.Log("Querying blockchain...");
            var request = GetRequest();
            if (request == null) throw new Exception("Failed to find the purchase in the last week of transactions.");

            log.Log($"Request started at {request.Block.Utc}");
            var contractEnd = RunToContractEnd(request);

            var requestTimeline = new TimeRange(request.Block.Utc.AddMinutes(-1.0), contractEnd.AddMinutes(1.0));
            log.Log($"Request timeline: {requestTimeline.From} -> {requestTimeline.To}");

            // For this timeline, we log all the calls to reserve-slot.
            var events = contracts.GetEvents(requestTimeline);

            events.GetReserveSlotCalls(call =>
            {
                if (IsThisRequest(call.RequestId))
                {
                    output.LogReserveSlotCall(call);
                    log.Log("Found reserve-slot call for slotIndex " + call.SlotIndex);
                }
            });

            log.Log("Writing blockchain output...");
            output.WriteContractEvents();

            return requestTimeline;
        }

        private DateTime RunToContractEnd(Request request)
        {
            var utc = request.Block.Utc.AddMinutes(-1.0);
            var tracker = new ChainRequestTracker(output, input.PurchaseId);
            var ignoreLog = new NullLog();
            var chainState = new ChainState(ignoreLog, contracts, tracker, utc, false);

            var atNow = false;
            while (!tracker.IsFinished && !atNow)
            {
                utc += TimeSpan.FromHours(1.0);
                if (utc > DateTime.UtcNow)
                {
                    log.Log("Caught up to present moment without finding contract end.");
                    utc = DateTime.UtcNow;
                    atNow = true;
                }

                log.Log($"Querying up to {utc}");
                chainState.Update(utc);
            }

            if (atNow) return utc;
            return tracker.FinishUtc;
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
}
