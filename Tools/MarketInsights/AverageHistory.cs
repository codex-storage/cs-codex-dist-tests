using CodexContractsPlugin;
using CodexContractsPlugin.ChainMonitor;
using Nethereum.Model;
using TestNetRewarder;
using Utils;

namespace MarketInsights
{
    public class AverageHistory : ITimeSegmentHandler
    {
        private readonly List<MarketTimeSegment> contributions = new List<MarketTimeSegment>();
        private readonly ChainStateChangeHandlerMux mux = new ChainStateChangeHandlerMux();
        private readonly AppState appState;
        private readonly int maxContributions;
        private readonly ChainState chainState;

        public AverageHistory(AppState appState, ICodexContracts contracts, int maxContributions)
        {
            this.appState = appState;
            this.maxContributions = maxContributions;
            chainState = new ChainState(appState.Log, contracts, mux, appState.Config.HistoryStartUtc);
        }

        public MarketTimeSegment[] Segments { get; private set; } = Array.Empty<MarketTimeSegment>();

        public Task<TimeSegmentResponse> OnNewSegment(TimeRange timeRange)
        {
            var contribution = BuildContribution(timeRange);
            contributions.Add(contribution);

            while (contributions.Count > maxContributions)
            {
                contributions.RemoveAt(0);
            }

            Segments = contributions.ToArray();

            return Task.FromResult(TimeSegmentResponse.OK);
        }

        private MarketTimeSegment BuildContribution(TimeRange timeRange)
        {
            var builder = new ContributionBuilder(timeRange);
            mux.Handlers.Add(builder);
            chainState.Update(timeRange.To);
            mux.Handlers.Remove(builder);
            return builder.GetSegment();
        }
    }
}
