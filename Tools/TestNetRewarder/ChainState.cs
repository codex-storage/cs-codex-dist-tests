using CodexContractsPlugin;
using CodexContractsPlugin.Marketplace;
using NethereumWorkflow.BlockUtils;
using Newtonsoft.Json;
using Utils;

namespace TestNetRewarder
{
    public class ChainState
    {
        private HistoricState historicState;
        private readonly string[] colorIcons = new[]
        {
            "🔴",
            "🟠",
            "🟡",
            "🟢",
            "🔵",
            "🟣",
            "🟤",
            "⚫",
            "⚪",
            "🟥",
            "🟧",
            "🟨",
            "🟩",
            "🟦",
            "🟪",
            "🟫",
            "⬛",
            "⬜",
            "🔶",
            "🔷"
        };

        public ChainState(HistoricState historicState, ICodexContracts contracts, BlockInterval blockRange)
        {
            this.historicState = historicState;

            NewRequests = contracts.GetStorageRequests(blockRange);
            historicState.CleanUpOldRequests();
            historicState.ProcessNewRequests(NewRequests);
            historicState.UpdateStorageRequests(contracts);

            StartedRequests = historicState.StorageRequests.Where(r => r.RecentlyStarted).ToArray();
            FinishedRequests = historicState.StorageRequests.Where(r => r.RecentlyFinished).ToArray();
            RequestFulfilledEvents = contracts.GetRequestFulfilledEvents(blockRange);
            RequestCancelledEvents = contracts.GetRequestCancelledEvents(blockRange);
            SlotFilledEvents = contracts.GetSlotFilledEvents(blockRange);
            SlotFreedEvents = contracts.GetSlotFreedEvents(blockRange);
        }

        public ChainState(
            Request[] newRequests,
            RequestFulfilledEventDTO[] requestFulfilledEvents,
            RequestCancelledEventDTO[] requestCancelledEvents,
             SlotFilledEventDTO[] slotFilledEvents,
             SlotFreedEventDTO[] slotFreedEvents)
        {
            NewRequests = newRequests;
            RequestFulfilledEvents = requestFulfilledEvents;
            RequestCancelledEvents = requestCancelledEvents;
            SlotFilledEvents = slotFilledEvents;
            SlotFreedEvents = slotFreedEvents;

            historicState = new HistoricState();
            StartedRequests = Array.Empty<StorageRequest>();
            FinishedRequests = Array.Empty<StorageRequest>();
        }

        public Request[] NewRequests { get; }
        [JsonIgnore]
        public StorageRequest[] AllRequests => historicState.StorageRequests;
        [JsonIgnore]
        public StorageRequest[] StartedRequests { get; private set; }
        [JsonIgnore]
        public StorageRequest[] FinishedRequests { get; private set; }
        public RequestFulfilledEventDTO[] RequestFulfilledEvents { get; }
        public RequestCancelledEventDTO[] RequestCancelledEvents { get; }
        public SlotFilledEventDTO[] SlotFilledEvents { get; }
        public SlotFreedEventDTO[] SlotFreedEvents { get; }

        public string EntireString()
        {
            return
                $"ChainState=[{JsonConvert.SerializeObject(this)}]" +
                $"HistoricState=[{historicState.EntireString()}]";
        }

        public void Set(HistoricState h)
        {
            historicState = h;
        }

        public string[] GenerateOverview()
        {
            var entries = new List<StringBlockNumberPair>();

            entries.AddRange(NewRequests.Select(ToPair));
            entries.AddRange(RequestFulfilledEvents.Select(ToPair));
            entries.AddRange(RequestCancelledEvents.Select(ToPair));
            entries.AddRange(SlotFilledEvents.Select(ToPair));
            entries.AddRange(SlotFreedEvents.Select(ToPair));
            entries.AddRange(FinishedRequests.Select(ToPair));

            entries.Sort(new StringUtcComparer());

            return entries.Select(ToLine).ToArray();
        }

        private StringBlockNumberPair ToPair(Request r)
        {
            return new StringBlockNumberPair("NewRequest", JsonConvert.SerializeObject(r), r.Block, r.RequestId);
        }

        public StringBlockNumberPair ToPair(StorageRequest r)
        {
            return new StringBlockNumberPair("FinishedRequest", JsonConvert.SerializeObject(r), r.Request.Block, r.Request.RequestId);
        }

        private StringBlockNumberPair ToPair(RequestFulfilledEventDTO r)
        {
            return new StringBlockNumberPair("Fulfilled", JsonConvert.SerializeObject(r), r.Block, r.RequestId);
        }

        private StringBlockNumberPair ToPair(RequestCancelledEventDTO r)
        {
            return new StringBlockNumberPair("Cancelled", JsonConvert.SerializeObject(r), r.Block, r.RequestId);
        }

        private StringBlockNumberPair ToPair(SlotFilledEventDTO r)
        {
            return new StringBlockNumberPair("SlotFilled", JsonConvert.SerializeObject(r), r.Block, r.RequestId);
        }

        private StringBlockNumberPair ToPair(SlotFreedEventDTO r)
        {
            return new StringBlockNumberPair("SlotFreed", JsonConvert.SerializeObject(r), r.Block, r.RequestId);
        }

        private string ToLine(StringBlockNumberPair pair)
        {
            var nl = Environment.NewLine;
            var colorIcon = GetColorIcon(pair.RequestId);
            return $"{colorIcon} {pair.Block} ({pair.Name}){nl}" +
                $"```json{nl}" +
                $"{pair.Str}{nl}" +
                $"```";
        }

        private string GetColorIcon(byte[] requestId)
        {
            var index = requestId[0] % colorIcons.Length;
            return colorIcons[index];
        }

        public class StringBlockNumberPair
        {
            public StringBlockNumberPair(string name, string str, BlockTimeEntry block, byte[] requestId)
            {
                Name = name;
                Str = str;
                Block = block;
                RequestId = requestId;
            }

            public string Name { get; }
            public string Str { get; }
            public BlockTimeEntry Block { get; }
            public byte[] RequestId { get; }
        }

        public class StringUtcComparer : IComparer<StringBlockNumberPair>
        {
            public int Compare(StringBlockNumberPair? x, StringBlockNumberPair? y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return 1;
                if (y == null) return -1;
                return x.Block.BlockNumber.CompareTo(y.Block.BlockNumber);
            }
        }
    }
}
