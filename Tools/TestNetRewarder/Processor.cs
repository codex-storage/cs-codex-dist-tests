using CodexContractsPlugin;
using CodexContractsPlugin.ChainMonitor;
using Logging;
using Utils;

namespace TestNetRewarder
{
    public class Processor : ITimeSegmentHandler
    {
        private readonly RequestBuilder builder;
        private readonly RewardChecker rewardChecker;
        private readonly MarketTracker marketTracker;
        private readonly EventsFormatter eventsFormatter;
        private readonly ChainState chainState;
        private readonly BotClient client;
        private readonly ILog log;

        public Processor(Configuration config, BotClient client, ICodexContracts contracts, ILog log)
        {
            this.client = client;
            this.log = log;

            builder = new RequestBuilder();
            rewardChecker = new RewardChecker(builder);
            marketTracker = new MarketTracker(config, log);
            eventsFormatter = new EventsFormatter();

            var handler = new ChainStateChangeHandlerMux(
                rewardChecker.Handler,
                marketTracker,
                eventsFormatter
            );

            chainState = new ChainState(log, contracts, handler, config.HistoryStartUtc);
        }

        public async Task OnNewSegment(TimeRange timeRange)
        {
            try
            {
                chainState.Update(timeRange.To);

                var averages = marketTracker.GetAverages();
                var events = eventsFormatter.GetEvents();

                var request = builder.Build(averages, events);
                if (request.HasAny())
                {
                    await client.SendRewards(request);
                }
            }
            catch (Exception ex)
            {
                var msg = "Exception processing time segment: " + ex;
                log.Error(msg); 
                eventsFormatter.AddError(msg);
                throw;
            }
        }
    }
}
