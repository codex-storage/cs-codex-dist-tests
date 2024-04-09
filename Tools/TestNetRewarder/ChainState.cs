using CodexContractsPlugin;
using CodexContractsPlugin.Marketplace;
using Newtonsoft.Json;
using Utils;

namespace TestNetRewarder
{
    public class ChainState
    {
        private readonly HistoricState historicState;

        public ChainState(HistoricState historicState, ICodexContracts contracts, BlockInterval blockRange)
        {
            NewRequests = contracts.GetStorageRequests(blockRange);
            historicState.CleanUpOldRequests();
            historicState.ProcessNewRequests(NewRequests);
            historicState.UpdateStorageRequests(contracts);

            StartedRequests = historicState.StorageRequests.Where(r => r.RecentlyStarted).ToArray();
            FinishedRequests = historicState.StorageRequests.Where(r => r.RecentlyFinished).ToArray();
            ChangedRequests = historicState.StorageRequests.Where(r => r.RecentlyChanged).ToArray();
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
        public StorageRequest[] ChangedRequests { get; private set; }
        public RequestFulfilledEventDTO[] RequestFulfilledEvents { get; }
        public RequestCancelledEventDTO[] RequestCancelledEvents { get; }
        public SlotFilledEventDTO[] SlotFilledEvents { get; }
        public SlotFreedEventDTO[] SlotFreedEvents { get; }

        public string[] GenerateOverview()
        {
            var entries = new List<StringBlockNumberPair>();

            entries.AddRange(ChangedRequests.Select(ToPair));
            entries.AddRange(RequestFulfilledEvents.Select(ToPair));
            entries.AddRange(RequestCancelledEvents.Select(ToPair));
            entries.AddRange(SlotFilledEvents.Select(ToPair));
            entries.AddRange(SlotFreedEvents.Select(ToPair));

            entries.Sort(new StringUtcComparer());

            return entries.Select(ToLine).ToArray();
        }

        private StringBlockNumberPair ToPair(StorageRequest r)
        {
            return new StringBlockNumberPair(JsonConvert.SerializeObject(r), r.Request.BlockNumber);
        }

        private StringBlockNumberPair ToPair(RequestFulfilledEventDTO r)
        {
            return new StringBlockNumberPair(JsonConvert.SerializeObject(r), r.BlockNumber);
        }

        private StringBlockNumberPair ToPair(RequestCancelledEventDTO r)
        {
            return new StringBlockNumberPair(JsonConvert.SerializeObject(r), r.BlockNumber);
        }

        private StringBlockNumberPair ToPair(SlotFilledEventDTO r)
        {
            return new StringBlockNumberPair(JsonConvert.SerializeObject(r), r.BlockNumber);
        }

        private StringBlockNumberPair ToPair(SlotFreedEventDTO r)
        {
            return new StringBlockNumberPair(JsonConvert.SerializeObject(r), r.BlockNumber);
        }

        private string ToLine(StringBlockNumberPair pair)
        {
            return $"[{pair.Number}] {pair.Str}";
        }

        public class StringBlockNumberPair
        {
            public StringBlockNumberPair(string str, ulong number)
            {
                Str = str;
                Number = number;
            }

            public string Str { get; }
            public ulong Number { get; }
        }

        public class StringUtcComparer : IComparer<StringBlockNumberPair>
        {
            public int Compare(StringBlockNumberPair? x, StringBlockNumberPair? y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return 1;
                if (y == null) return -1;
                return x.Number.CompareTo(y.Number);
            }
        }
    }
}
