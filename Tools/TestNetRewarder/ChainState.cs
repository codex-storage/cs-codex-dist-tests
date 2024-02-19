using CodexContractsPlugin;
using CodexContractsPlugin.Marketplace;
using BlockRange = Utils.BlockRange;

namespace TestNetRewarder
{
    public class ChainState
    {
        private readonly HistoricState historicState;

        public ChainState(HistoricState historicState, ICodexContracts contracts, BlockRange blockRange)
        {
            NewRequests = contracts.GetStorageRequests(blockRange);
            historicState.ProcessNewRequests(NewRequests);
            historicState.UpdateStorageRequests(contracts);

            StartedRequests = historicState.StorageRequests.Where(r => r.RecentlyStarted).ToArray();
            FinishedRequests = historicState.StorageRequests.Where(r => r.RecentlyFininshed).ToArray();
            RequestFulfilledEvents = contracts.GetRequestFulfilledEvents(blockRange);
            RequestCancelledEvents = contracts.GetRequestCancelledEvents(blockRange);
            SlotFilledEvents = contracts.GetSlotFilledEvents(blockRange);
            SlotFreedEvents = contracts.GetSlotFreedEvents(blockRange);
            this.historicState = historicState;
        }

        public Request[] NewRequests { get; }
        public StorageRequest[] AllRequests => historicState.StorageRequests;
        public StorageRequest[] StartedRequests { get; private set; }
        public StorageRequest[] FinishedRequests { get; private set; }
        public RequestFulfilledEventDTO[] RequestFulfilledEvents { get; }
        public RequestCancelledEventDTO[] RequestCancelledEvents { get; }
        public SlotFilledEventDTO[] SlotFilledEvents { get; }
        public SlotFreedEventDTO[] SlotFreedEvents { get; }
    }
}
