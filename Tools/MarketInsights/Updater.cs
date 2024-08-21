using CodexContractsPlugin;
using TestNetRewarder;

namespace MarketInsights
{
    public class Updater
    {
        private readonly Random random = new Random();
        private readonly AppState appState;
        private readonly CancellationToken ct;
        private readonly Tracker[] trackers;
        private readonly AverageHistory averageHistory;

        public Updater(AppState appState, ICodexContracts contracts, CancellationToken ct)
        {
            this.appState = appState;
            this.ct = ct;

            trackers = CreateTrackers();
            averageHistory = new AverageHistory(appState, contracts, trackers.Max(t => t.NumberOfSegments));
        }

        private Tracker[] CreateTrackers()
        {
            var tokens = appState.Config.TimeSegments.Split(";", StringSplitOptions.RemoveEmptyEntries);
            var nums = tokens.Select(t => Convert.ToInt32(t)).ToArray();
            return nums.Select(n => new Tracker(n, averageHistory)).ToArray();
        }

        public void Run()
        {
            Task.Run(Runner);
        }

        private async Task Runner()
        {
            var segmenter = new TimeSegmenter(
                appState.Log,
                segmentSize: appState.Config.UpdateInterval,
                historyStartUtc: appState.Config.HistoryStartUtc,
                handler: averageHistory
            );

            while (!ct.IsCancellationRequested)
            {
                await segmenter.ProcessNextSegment();
                await Task.Delay(TimeSpan.FromSeconds(3), ct);

                var marketTimeSegments = trackers
                    .Select(t => t.CreateMarketTimeSegment())
                    .Where(t => t != null)
                    .Cast<MarketTimeSegment>()  
                    .ToArray();

                appState.MarketOverview = new MarketOverview
                {
                    TimeSegments = marketTimeSegments,
                    IsUpToDate = segmenter.IsRealtime,
                    LastUpdatedUtc = DateTime.UtcNow
                };

                var r = random.Next(appState.Config.MaxRandomIntervalSeconds);
                await Task.Delay(TimeSpan.FromSeconds(r), ct);
            }
        }
    }
}
