using CodexContractsPlugin.ChainMonitor;
using Utils;
using YamlDotNet.Core;

namespace MarketInsights
{
    public class Tracker
    {
        private readonly AverageHistory history;

        public Tracker(int numberOfSegments, AverageHistory history)
        {
            NumberOfSegments = numberOfSegments;
            this.history = history;
        }

        public int NumberOfSegments { get; }

        public MarketTimeSegment? CreateMarketTimeSegment()
        {
            if (history.Segments.Length < NumberOfSegments) return null;

            var mySegments = history.Segments.TakeLast(NumberOfSegments);
            return AverageSegments(mySegments);
        }

        private MarketTimeSegment AverageSegments(IEnumerable<MarketTimeSegment> mySegments)
        {
            var result = new MarketTimeSegment();

            foreach (var segment in mySegments)
            {
                result.FromUtc = Min(result.FromUtc, segment.FromUtc);
                result.ToUtc = Max(result.ToUtc, segment.ToUtc);

                Combine(result.Submitted, segment.Submitted);
                Combine(result.Expired, segment.Expired);
                Combine(result.Started, segment.Started);
                Combine(result.Finished, segment.Finished);
                Combine(result.Failed, segment.Failed);
            }
            return result;
        }

        private void Combine(ContractAverages result, ContractAverages toAdd)
        {
            float weight1 = result.Number;
            float weight2 = toAdd.Number;

            result.Price = RollingAverage.GetWeightedAverage(result.Price, weight1, toAdd.Price, weight2);
            result.Size = RollingAverage.GetWeightedAverage(result.Size, weight1, toAdd.Size, weight2);
            result.Duration = RollingAverage.GetWeightedAverage(result.Duration, weight1, toAdd.Duration, weight2);
            result.Collateral = RollingAverage.GetWeightedAverage(result.Collateral, weight1, toAdd.Collateral, weight2);
            result.ProofProbability = RollingAverage.GetWeightedAverage(result.ProofProbability, weight1, toAdd.ProofProbability, weight2);
        }

        private DateTime Max(DateTime a, DateTime b)
        {
            if (a > b) return a;
            return b;
        }

        private DateTime Min(DateTime a, DateTime b)
        {
            if (a > b) return b;
            return a;
        }
    }
}
