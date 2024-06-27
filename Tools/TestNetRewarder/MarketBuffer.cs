using CodexContractsPlugin.ChainMonitor;
using CodexContractsPlugin.Marketplace;
using DiscordRewards;
using System.Numerics;

namespace TestNetRewarder
{
    public class MarketBuffer
    {
        private readonly List<RequestEvent> requestEvents = new List<RequestEvent>();
        private readonly TimeSpan bufferSpan;

        public MarketBuffer(TimeSpan bufferSpan)
        {
            this.bufferSpan = bufferSpan;
        }

        public void Add(RequestEvent requestEvent)
        {
            requestEvents.Add(requestEvent);
        }

        public void Update()
        {
            var now = DateTime.UtcNow;
            requestEvents.RemoveAll(r => (now - r.Request.FinishedUtc) > bufferSpan);
        }

        public MarketAverage? GetAverage()
        {
            if (requestEvents.Count == 0) return null;

            return new MarketAverage
            {
                NumberOfFinished = requestEvents.Count,
                TimeRangeSeconds = (int)bufferSpan.TotalSeconds,
                Price = Average(s => s.Request.Ask.Reward),
                Duration = Average(s => s.Request.Ask.Duration),
                Size = Average(s => GetTotalSize(s.Request.Ask)),
                Collateral = Average(s => s.Request.Ask.Collateral),
                ProofProbability = Average(s => s.Request.Ask.ProofProbability)
            };
        }

        private float Average(Func<IChainStateRequest, BigInteger> getValue)
        {
            return Average(s =>
            {
                var value = getValue(s);
                return (int)value;
            });
        }

        private float Average(Func<IChainStateRequest, int> getValue)
        {
            var sum = 0.0f;
            float count = requestEvents.Count;
            foreach (var r in requestEvents)
            {
                sum += getValue(r.Request);
            }

            if (count < 1.0f) return 0.0f;
            return sum / count;
        }

        private int GetTotalSize(Ask ask)
        {
            var nSlots = Convert.ToInt32(ask.Slots);
            var slotSize = (int)ask.SlotSize;
            return nSlots * slotSize;
        }
    }
}
