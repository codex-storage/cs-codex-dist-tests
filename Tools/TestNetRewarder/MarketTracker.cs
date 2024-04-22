using CodexContractsPlugin.Marketplace;
using DiscordRewards;
using System.Numerics;

namespace TestNetRewarder
{
    public class MarketTracker
    {
        private readonly MarketAverage MostRecent = new MarketAverage
        {
            Title = "Most recent"
        };
        private readonly MarketAverage Irf = new MarketAverage
        {
            Title = "Recent average"
        };

        public MarketAverage[] ProcessChainState(ChainState chainState)
        {
            UpdateMostRecent(chainState);
            UpdateIrf(chainState);

            return new[]
            {
                MostRecent,
                Irf
            };
        }

        private void UpdateIrf(ChainState chainState)
        {
            if (!chainState.FinishedRequests.Any()) return;

            MostRecent.Price = GetIrf(MostRecent.Price, chainState, s => s.Request.Ask.Reward);
            MostRecent.Duration = GetIrf(MostRecent.Duration, chainState, s => s.Request.Ask.Duration);
            MostRecent.Size = GetIrf(MostRecent.Size, chainState, s => GetTotalSize(s.Request.Ask));
            MostRecent.Collateral = GetIrf(MostRecent.Collateral, chainState, s => s.Request.Ask.Collateral);
            MostRecent.ProofProbability = GetIrf(MostRecent.ProofProbability, chainState, s => s.Request.Ask.ProofProbability);
        }

        private void UpdateMostRecent(ChainState chainState)
        {
            if (!chainState.FinishedRequests.Any()) return;

            MostRecent.Price = Average(chainState, s => s.Request.Ask.Reward);
            MostRecent.Duration = Average(chainState, s => s.Request.Ask.Duration);
            MostRecent.Size = Average(chainState, s => GetTotalSize(s.Request.Ask));
            MostRecent.Collateral = Average(chainState, s => s.Request.Ask.Collateral);
            MostRecent.ProofProbability = Average(chainState, s => s.Request.Ask.ProofProbability);
        }

        private int GetTotalSize(Ask ask)
        {
            var nSlots = Convert.ToInt32(ask.Slots);
            var slotSize = Convert.ToInt32(ask.SlotSize);
            return nSlots * slotSize;
        }

        private float Average(ChainState state, Func<StorageRequest, BigInteger> getValue)
        {
            return Average(state, s => Convert.ToInt32(getValue(s)));
        }

        private float GetIrf(float current, ChainState state, Func<StorageRequest, BigInteger> getValue)
        {
            return GetIrf(current, state, s => Convert.ToInt32(getValue(s)));
        }

        private float Average(ChainState state, Func<StorageRequest, int> getValue)
        {
            var sum = 0.0f;
            var count = 0.0f;
            foreach (var finishedRequest in state.FinishedRequests)
            {
                sum += getValue(finishedRequest);
                count++;
            }

            if (count < 1.0f) return 0.0f;
            return sum / count;
        }

        private float GetIrf(float current, ChainState state, Func<StorageRequest, int> getValue)
        {
            var result = current;
            foreach (var finishedRequest in state.FinishedRequests)
            {
                float v = getValue(finishedRequest);
                result = (result + v) / 2.0f;
            }

            return result;
        }
    }
}
