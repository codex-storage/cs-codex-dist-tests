using CodexContractsPlugin.Marketplace;
using Logging;
using System.Numerics;
using Utils;

namespace CodexContractsPlugin.ChainMonitor
{
    public interface IChainStateChangeHandler
    {
        void OnNewRequest(IChainStateRequest request);
        void OnRequestStarted(IChainStateRequest request);
        void OnRequestFinished(IChainStateRequest request);
        void OnRequestFulfilled(IChainStateRequest request);
        void OnRequestCancelled(IChainStateRequest request);
        void OnSlotFilled(IChainStateRequest request, BigInteger slotIndex);
        void OnSlotFreed(IChainStateRequest request, BigInteger slotIndex);
    }

    public class ChainState
    {
        private readonly List<ChainStateRequest> requests = new List<ChainStateRequest>();
        private readonly ILog log;
        private readonly IChainStateChangeHandler handler;

        private ChainState(ILog log, IChainStateChangeHandler changeHandler, TimeRange timeRange)
        {
            this.log = new LogPrefixer(log, "(ChainState) ");
            handler = changeHandler;
            TotalSpan = timeRange;
        }

        public static ChainState FromTimeRange(ILog log, ICodexContracts contracts, TimeRange timeRange, IChainStateChangeHandler changeHandler)
        {
            var events = ChainEvents.FromTimeRange(contracts, timeRange);
            return FromEvents(log, events, changeHandler);
        }

        public static ChainState FromEvents(ILog log, ChainEvents events, IChainStateChangeHandler changeHandler)
        {
            var state = new ChainState(log, changeHandler, events.BlockInterval.TimeRange);
            state.Apply(events);
            return state;
        }

        public TimeRange TotalSpan { get; private set; }
        public IChainStateRequest[] Requests => requests.ToArray();

        public void Update(ICodexContracts contracts)
        {
            Update(contracts, DateTime.UtcNow);
        }

        public void Update(ICodexContracts contracts, DateTime toUtc)
        {
            var span = new TimeRange(TotalSpan.To, toUtc);
            var events = ChainEvents.FromTimeRange(contracts, span);
            Apply(events);

            TotalSpan = new TimeRange(TotalSpan.From, span.To);
        }

        private void Apply(ChainEvents events)
        {
            if (events.BlockInterval.TimeRange.From < TotalSpan.From)
                throw new Exception("Attempt to update ChainState with set of events from before its current record.");

            log.Log($"ChainState updating: {events.BlockInterval}");

            // Run through each block and apply the events to the state in order.
            var span = events.BlockInterval.TimeRange.Duration;
            var numBlocks = events.BlockInterval.NumberOfBlocks;
            var spanPerBlock = span / numBlocks;

            var eventUtc = events.BlockInterval.TimeRange.From;
            for (var b = events.BlockInterval.From; b <= events.BlockInterval.To; b++)
            {
                var blockEvents = events.All.Where(e => e.Block.BlockNumber == b).ToArray();
                ApplyEvents(b, blockEvents, eventUtc);

                eventUtc += spanPerBlock;
            }
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

        private void ApplyEvent(Request request)
        {
            if (requests.Any(r => Equal(r.Request.RequestId, request.RequestId)))
                throw new Exception("Received NewRequest event for id that already exists.");

            var newRequest = new ChainStateRequest(log, request, RequestState.New);
            requests.Add(newRequest);

            handler.OnNewRequest(newRequest);
        }

        private void ApplyEvent(RequestFulfilledEventDTO request)
        {
            var r = FindRequest(request.RequestId);
            if (r == null) return;
            r.UpdateState(request.Block.BlockNumber, RequestState.Started);
            handler.OnRequestFulfilled(r);
        }

        private void ApplyEvent(RequestCancelledEventDTO request)
        {
            var r = FindRequest(request.RequestId);
            if (r == null) return;
            r.UpdateState(request.Block.BlockNumber, RequestState.Cancelled);
            handler.OnRequestCancelled(r);
        }

        private void ApplyEvent(SlotFilledEventDTO request)
        {
            var r = FindRequest(request.RequestId);
            if (r == null) return;
            r.Log($"[{request.Block.BlockNumber}] SlotFilled");
            handler.OnSlotFilled(r, request.SlotIndex);
        }

        private void ApplyEvent(SlotFreedEventDTO request)
        {
            var r = FindRequest(request.RequestId);
            if (r == null) return;
            r.Log($"[{request.Block.BlockNumber}] SlotFreed");
            handler.OnSlotFreed(r, request.SlotIndex);
        }

        private void ApplyTimeImplicitEvents(ulong blockNumber, DateTime eventsUtc)
        {
            foreach (var r in requests)
            {
                if (r.State == RequestState.Started
                    && r.FinishedUtc < eventsUtc)
                {
                    r.UpdateState(blockNumber, RequestState.Finished);
                    handler.OnRequestFinished(r);
                }
            }
        }

        private ChainStateRequest? FindRequest(byte[] requestId)
        {
            var r = requests.SingleOrDefault(r => Equal(r.Request.RequestId, requestId));
            if (r == null) log.Log("Unable to find request by ID!");
            return r;
        }

        private bool Equal(byte[] a, byte[] b)
        {
            return a.SequenceEqual(b);
        }
    }
}
