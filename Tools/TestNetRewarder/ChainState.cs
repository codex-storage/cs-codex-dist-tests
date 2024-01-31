using CodexContractsPlugin;
using CodexContractsPlugin.Marketplace;
using Utils;

namespace TestNetRewarder
{
    public class ChainState
    {
        private readonly HistoricState historicState;

        public ChainState(HistoricState historicState, ICodexContracts contracts, TimeRange timeRange)
        {
            NewRequests = contracts.GetStorageRequests(timeRange);
            historicState.ProcessNewRequests(NewRequests);
            historicState.UpdateStorageRequests(contracts);

            StartedRequests = historicState.StorageRequests.Where(r => r.RecentlyStarted).ToArray();
            FinishedRequests = historicState.StorageRequests.Where(r => r.RecentlyFininshed).ToArray();
            RequestFulfilledEvents = contracts.GetRequestFulfilledEvents(timeRange);
            RequestCancelledEvents = contracts.GetRequestCancelledEvents(timeRange);
            SlotFilledEvents = contracts.GetSlotFilledEvents(timeRange);
            SlotFreedEvents = contracts.GetSlotFreedEvents(timeRange);
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
