using CodexContractsPlugin.Marketplace;
using DiscordRewards;
using System.Numerics;

namespace TestNetRewarder
{
    public class MarketTracker
    {
        private readonly List<ChainState> buffer = new List<ChainState>();

        public MarketAverage[] ProcessChainState(ChainState chainState)
        {
            var intervalCounts = GetInsightCounts();
            if (!intervalCounts.Any()) return Array.Empty<MarketAverage>();

            UpdateBuffer(chainState, intervalCounts.Max());
            var result = intervalCounts
                .Select(GenerateMarketAverage)
                .Where(a => a != null)
                .Cast<MarketAverage>()
                .ToArray();

            if (!result.Any()) result = Array.Empty<MarketAverage>();
            return result;
        }

        private void UpdateBuffer(ChainState chainState, int maxNumberOfIntervals)
        {
            buffer.Add(chainState);
            while (buffer.Count > maxNumberOfIntervals)
            {
                buffer.RemoveAt(0);
            }
        }

        private MarketAverage? GenerateMarketAverage(int numberOfIntervals)
        {
            var states = SelectStates(numberOfIntervals);
            return CreateAverage(states);
        }

        private ChainState[] SelectStates(int numberOfIntervals)
        {
            if (numberOfIntervals < 1) return Array.Empty<ChainState>();
            return buffer.TakeLast(numberOfIntervals).ToArray();
        }

        private MarketAverage? CreateAverage(ChainState[] states)
        {
            try
            {
                return new MarketAverage
                {
                    NumberOfFinished = CountNumberOfFinishedRequests(states),
                    TimeRange = GetTotalTimeRange(states),
                    Price = Average(states, s => s.Request.Ask.Reward),
                    Duration = Average(states, s => s.Request.Ask.Duration),
                    Size = Average(states, s => GetTotalSize(s.Request.Ask)),
                    Collateral = Average(states, s => s.Request.Ask.Collateral),
                    ProofProbability = Average(states, s => s.Request.Ask.ProofProbability)
                };
            }
            catch (Exception ex)
            {
                Program.Log.Error($"Exception in CreateAverage: {ex}");
                return null;
            }
        }

        private int GetTotalSize(Ask ask)
        {
            var nSlots = Convert.ToInt32(ask.Slots);
            var slotSize = Convert.ToInt32(ask.SlotSize);
            return nSlots * slotSize;
        }

        private float Average(ChainState[] states, Func<StorageRequest, BigInteger> getValue)
        {
            return Average(states, s => Convert.ToInt32(getValue(s)));
        }

        private float Average(ChainState[] states, Func<StorageRequest, int> getValue)
        {
            var sum = 0.0f;
            var count = 0.0f;
            foreach (var state in states)
            {
                foreach (var finishedRequest in state.FinishedRequests)
                {
                    sum += getValue(finishedRequest);
                    count++;
                }
            }

            return sum / count;
        }

        private TimeSpan GetTotalTimeRange(ChainState[] states)
        {
            return Program.Config.Interval * states.Length;
        }

        private int CountNumberOfFinishedRequests(ChainState[] states)
        {
            return states.Sum(s => s.FinishedRequests.Length);
        }

        private int[] GetInsightCounts()
        {
            try
            {
                var tokens = Program.Config.MarketInsights.Split(';').ToArray();
                return tokens.Select(t => Convert.ToInt32(t)).ToArray();
            }
            catch (Exception ex)
            {
                Program.Log.Error($"Exception when parsing MarketInsights config parameters: {ex}");
            }
            return Array.Empty<int>();            
        }
    }
}
