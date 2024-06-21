using CodexContractsPlugin.ChainMonitor;
using CodexContractsPlugin.Marketplace;
using DiscordRewards;
using System.Numerics;

namespace TestNetRewarder
{
    public class MarketBuffer
    {
        private readonly List<IChainStateRequest> requests = new List<IChainStateRequest>();
        private readonly TimeSpan bufferSpan;

        public MarketBuffer(TimeSpan bufferSpan)
        {
            this.bufferSpan = bufferSpan;
        }

        public void Add(IChainStateRequest request)
        {
            requests.Add(request);
        }

        public void Update()
        {
            var now = DateTime.UtcNow;
            requests.RemoveAll(r => (now - r.FinishedUtc) > bufferSpan);
        }

        public MarketAverage? GetAverage()
        {
            if (requests.Count == 0) return null;

            return new MarketAverage
            {
                NumberOfFinished = requests.Count,
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
            float count = requests.Count;
            foreach (var r in requests)
            {
                sum += getValue(r);
            }

            if (count < 1.0f) return 0.0f;
            return sum / count;
        }

        private int GetTotalSize(Ask ask)
        {
            var nSlots = Convert.ToInt32(ask.Slots);
            var slotSize = Convert.ToInt32(ask.SlotSize);
            return nSlots * slotSize;
        }
    }
}
