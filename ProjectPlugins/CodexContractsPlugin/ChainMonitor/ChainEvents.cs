using CodexContractsPlugin.Marketplace;
using System.Collections.Generic;
using Utils;

namespace CodexContractsPlugin.ChainMonitor
{
    public class ChainEvents
    {
        private ChainEvents(
            BlockInterval blockInterval,
            Request[] requests,
            RequestFulfilledEventDTO[] fulfilled,
            RequestCancelledEventDTO[] cancelled,
            RequestFailedEventDTO[] failed,
            SlotFilledEventDTO[] slotFilled,
            SlotFreedEventDTO[] slotFreed,
            SlotReservationsFullEventDTO[] slotReservationsFull,
            ProofSubmittedEventDTO[] proofSubmitted
            )
        {
            BlockInterval = blockInterval;
            Requests = requests;
            Fulfilled = fulfilled;
            Cancelled = cancelled;
            Failed = failed;
            SlotFilled = slotFilled;
            SlotFreed = slotFreed;
            SlotReservationsFull = slotReservationsFull;
            ProofSubmitted = proofSubmitted;
            All = ConcatAll<IHasBlock>(requests, fulfilled, cancelled, failed, slotFilled, SlotFreed, SlotReservationsFull, ProofSubmitted);
        }

        public BlockInterval BlockInterval { get; }
        public Request[] Requests { get; }
        public RequestFulfilledEventDTO[] Fulfilled { get; }
        public RequestCancelledEventDTO[] Cancelled { get; }
        public RequestFailedEventDTO[] Failed { get; }
        public SlotFilledEventDTO[] SlotFilled { get; }
        public SlotFreedEventDTO[] SlotFreed { get; }
        public SlotReservationsFullEventDTO[] SlotReservationsFull { get; }
        public ProofSubmittedEventDTO[] ProofSubmitted { get; }
        public IHasBlock[] All { get; }

        public static ChainEvents FromBlockInterval(ICodexContracts contracts, BlockInterval blockInterval)
        {
            return FromContractEvents(contracts.GetEvents(blockInterval));
        }

        public static ChainEvents FromTimeRange(ICodexContracts contracts, TimeRange timeRange)
        {
            return FromContractEvents(contracts.GetEvents(timeRange));
        }

        public static ChainEvents FromContractEvents(ICodexContractsEvents events)
        {
            return new ChainEvents(
                events.BlockInterval,
                events.GetStorageRequests(),
                events.GetRequestFulfilledEvents(),
                events.GetRequestCancelledEvents(),
                events.GetRequestFailedEvents(),
                events.GetSlotFilledEvents(),
                events.GetSlotFreedEvents(),
                events.GetSlotReservationsFullEvents(),
                events.GetProofSubmittedEvents()
            );
        }

        private T[] ConcatAll<T>(params T[][] arrays)
        {
            var result = Array.Empty<T>();
            foreach (var array in arrays)
            {
                result = result.Concat(array).ToArray();
            }
            return result;
        }
    }
}
