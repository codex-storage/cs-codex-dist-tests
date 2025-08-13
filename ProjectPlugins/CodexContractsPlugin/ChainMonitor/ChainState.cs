using BlockchainUtils;
using CodexContractsPlugin.Marketplace;
using Logging;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Numerics;
using Utils;

namespace CodexContractsPlugin.ChainMonitor
{
    public interface IChainStateChangeHandler
    {
        void OnNewRequest(RequestEvent requestEvent);
        void OnRequestFinished(RequestEvent requestEvent);
        void OnRequestFulfilled(RequestEvent requestEvent);
        void OnRequestCancelled(RequestEvent requestEvent);
        void OnRequestFailed(RequestEvent requestEvent);
        void OnSlotFilled(RequestEvent requestEvent, EthAddress host, BigInteger slotIndex, bool isRepair);
        void OnSlotFreed(RequestEvent requestEvent, BigInteger slotIndex);
        void OnSlotReservationsFull(RequestEvent requestEvent, BigInteger slotIndex);
        void OnProofSubmitted(BlockTimeEntry block, string id);
        void OnError(string msg);
    }

    public class RequestEvent
    {
        public RequestEvent(BlockTimeEntry block, IChainStateRequest request)
        {
            Block = block;
            Request = request;
        }

        public BlockTimeEntry Block { get; }
        public IChainStateRequest Request { get; }
    }

    public class ChainState
    {
        private readonly List<ChainStateRequest> requests = new List<ChainStateRequest>();
        private readonly ILog log;
        private readonly ICodexContracts contracts;
        private readonly IChainStateChangeHandler handler;
        private readonly bool doProofPeriodMonitoring;

        public ChainState(ILog log, ICodexContracts contracts, IChainStateChangeHandler changeHandler, DateTime startUtc, bool doProofPeriodMonitoring)
        {
            this.log = new LogPrefixer(log, "(ChainState) ");
            this.contracts = contracts;
            handler = changeHandler;
            this.doProofPeriodMonitoring = doProofPeriodMonitoring;
            TotalSpan = new TimeRange(startUtc, startUtc);
            PeriodMonitor = new PeriodMonitor(log, contracts);
        }

        public TimeRange TotalSpan { get; private set; }
        public IChainStateRequest[] Requests => requests.ToArray();
        public PeriodMonitor PeriodMonitor { get; }

        public int Update()
        {
            return Update(DateTime.UtcNow);
        }

        public int Update(DateTime toUtc)
        {
            var span = new TimeRange(TotalSpan.To, toUtc);
            var events = ChainEvents.FromTimeRange(contracts, span);
            Apply(events);

            TotalSpan = new TimeRange(TotalSpan.From, span.To);
            return events.All.Length;
        }

        private void Apply(ChainEvents events)
        {
            if (events.BlockInterval.TimeRange.From < TotalSpan.From)
            {
                var msg = "Attempt to update ChainState with set of events from before its current record.";
                handler.OnError(msg);
                throw new Exception(msg);
            }

            log.Debug($"ChainState updating: {events.BlockInterval} = {events.All.Length} events.");

            // Run through each block and apply the events to the state in order.
            var span = events.BlockInterval.TimeRange.Duration;
            var numBlocks = events.BlockInterval.NumberOfBlocks;
            if (numBlocks == 0) return;
            var spanPerBlock = span / numBlocks;

            var eventUtc = events.BlockInterval.TimeRange.From;
            for (var b = events.BlockInterval.From; b <= events.BlockInterval.To; b++)
            {
                var blockEvents = events.All.Where(e => e.Block.BlockNumber == b).ToArray();
                ApplyEvents(b, blockEvents, eventUtc);
                UpdatePeriodMonitor(eventUtc);

                eventUtc += spanPerBlock;
            }
        }

        private void UpdatePeriodMonitor(DateTime eventUtc)
        {
            if (!doProofPeriodMonitoring) return;
            var activeRequests = requests.Where(r => r.State == RequestState.Started).ToArray();
            PeriodMonitor.Update(eventUtc, activeRequests);
        }

        private void ApplyEvents(ulong blockNumber, IHasBlock[] blockEvents, DateTime eventsUtc)
        {
            foreach (var e in blockEvents)
            {
                dynamic d = e;
                ApplyEvent(d);
            }

            ApplyTimeImplicitEvents(blockNumber, eventsUtc);
        }

        private void ApplyEvent(StorageRequestedEventDTO @event)
        {
            if (requests.Any(r => Equal(r.RequestId, @event.RequestId)))
            {
                var r = FindRequest(@event);
                if (r == null) throw new Exception("ChainState is inconsistent. Received already-known requestId that's not known.");
                if (@event.Block.BlockNumber != @event.Block.BlockNumber) throw new Exception("Same request found in different blocks.");
                log.Log("Received the same request-creation event multiple times.");
                return;
            }

            var request = contracts.GetRequest(@event.RequestId);
            var newRequest = new ChainStateRequest(log, @event.RequestId, @event.Block, request, RequestState.New);
            requests.Add(newRequest);

            handler.OnNewRequest(new RequestEvent(@event.Block, newRequest));
        }

        private void ApplyEvent(RequestFulfilledEventDTO @event)
        {
            var r = FindRequest(@event);
            if (r == null) return;
            r.UpdateState(@event.Block.BlockNumber, RequestState.Started);
            handler.OnRequestFulfilled(new RequestEvent(@event.Block, r));
        }

        private void ApplyEvent(RequestCancelledEventDTO @event)
        {
            var r = FindRequest(@event);
            if (r == null) return;
            r.UpdateState(@event.Block.BlockNumber, RequestState.Cancelled);
            handler.OnRequestCancelled(new RequestEvent(@event.Block, r));
        }

        private void ApplyEvent(RequestFailedEventDTO @event)
        {
            var r = FindRequest(@event);
            if (r == null) return;
            r.UpdateState(@event.Block.BlockNumber, RequestState.Failed);
            handler.OnRequestFailed(new RequestEvent(@event.Block, r));
        }

        private void ApplyEvent(SlotFilledEventDTO @event)
        {
            var r = FindRequest(@event);
            if (r == null) return;
            var slotIndex = (int)@event.SlotIndex;
            var isRepair = !r.Hosts.IsFilled(slotIndex) && r.Hosts.WasPreviouslyFilled(slotIndex);
            r.Hosts.HostFillsSlot(@event.Host, slotIndex);
            r.Log($"[{@event.Block.BlockNumber}] SlotFilled (host:'{@event.Host}', slotIndex:{@event.SlotIndex})");
            handler.OnSlotFilled(new RequestEvent(@event.Block, r), @event.Host, @event.SlotIndex, isRepair);
        }

        private void ApplyEvent(SlotFreedEventDTO @event)
        {
            var r = FindRequest(@event);
            if (r == null) return;
            r.Hosts.SlotFreed((int)@event.SlotIndex);
            r.Log($"[{@event.Block.BlockNumber}] SlotFreed (slotIndex:{@event.SlotIndex})");
            handler.OnSlotFreed(new RequestEvent(@event.Block, r), @event.SlotIndex);
        }

        private void ApplyEvent(SlotReservationsFullEventDTO @event)
        {
            var r = FindRequest(@event);
            if (r == null) return;
            r.Log($"[{@event.Block.BlockNumber}] SlotReservationsFull (slotIndex:{@event.SlotIndex})");
            handler.OnSlotReservationsFull(new RequestEvent(@event.Block, r), @event.SlotIndex);
        }

        private void ApplyEvent(ProofSubmittedEventDTO @event)
        {
            var id = Base58.Encode(@event.Id);

            var proofOrigin = SearchForProofOrigin(id);

            log.Log($"[{@event.Block.BlockNumber}] Proof submitted (id:{id} {proofOrigin})");
            handler.OnProofSubmitted(@event.Block, id);
        }

        private string SearchForProofOrigin(string slotId)
        {
            foreach (var r in requests)
            {
                for (decimal slotIndex = 0; slotIndex < r.Request.Ask.Slots; slotIndex++)
                {
                    var thisSlotId = contracts.GetSlotId(r.RequestId, slotIndex);
                    var id = Base58.Encode(thisSlotId);

                    if (id.ToLowerInvariant() == slotId.ToLowerInvariant())
                    {
                        return $"({r.RequestId.ToHex()} slotIndex:{slotIndex})";
                    }
                }
            }
            return "(Could not identify proof requestId + slot)";
        }

        private void ApplyTimeImplicitEvents(ulong blockNumber, DateTime eventsUtc)
        {
            foreach (var r in requests)
            {
                if (r.State == RequestState.Started
                    && r.FinishedUtc < eventsUtc)
                {
                    r.UpdateState(blockNumber, RequestState.Finished);
                    handler.OnRequestFinished(new RequestEvent(new BlockTimeEntry(blockNumber, eventsUtc), r));
                }
            }
        }

        private ChainStateRequest? FindRequest(IHasBlockAndRequestId hasBoth)
        {
            var r = requests.SingleOrDefault(r => Equal(r.RequestId, hasBoth.RequestId));
            if (r != null) return r;
           
            try
            {
                var req = contracts.GetRequest(hasBoth.RequestId);
                var state = contracts.GetRequestState(hasBoth.RequestId);
                var newRequest = new ChainStateRequest(log, hasBoth.RequestId, hasBoth.Block, req, state);
                requests.Add(newRequest);
                return newRequest;
            }
            catch (Exception ex)
            {
                var msg = $"Failed to get request with id '{hasBoth.RequestId.ToHex()}' from chain: {ex}";
                log.Error(msg);
                handler.OnError(msg);
                return null;
            }
        }

        private bool Equal(byte[] a, byte[] b)
        {
            return a.SequenceEqual(b);
        }
    }
}
