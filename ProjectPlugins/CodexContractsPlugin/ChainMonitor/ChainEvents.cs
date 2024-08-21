using CodexContractsPlugin.Marketplace;
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
            SlotFreedEventDTO[] slotFreed
            )
        {
            BlockInterval = blockInterval;
            Requests = requests;
            Fulfilled = fulfilled;
            Cancelled = cancelled;
            Failed = failed;
            SlotFilled = slotFilled;
            SlotFreed = slotFreed;
        }

        public BlockInterval BlockInterval { get; }
        public Request[] Requests { get; }
        public RequestFulfilledEventDTO[] Fulfilled { get; }
        public RequestCancelledEventDTO[] Cancelled { get; }
        public RequestFailedEventDTO[] Failed { get; }
        public SlotFilledEventDTO[] SlotFilled { get; }
        public SlotFreedEventDTO[] SlotFreed { get; }

        public IHasBlock[] All
        {
            get
            {
                var all = new List<IHasBlock>();
                all.AddRange(Requests);
                all.AddRange(Fulfilled);
                all.AddRange(Cancelled);
                all.AddRange(Failed);
                all.AddRange(SlotFilled);
                all.AddRange(SlotFreed);
                return all.ToArray();
            }
        }

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
                events.GetSlotFreedEvents()
            );
        }
    }
}
