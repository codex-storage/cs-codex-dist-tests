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
            eventsFormatter = new EventsFormatter();

            var handler = new ChainStateChangeHandlerMux(
                rewardChecker.Handler,
                eventsFormatter
            );

            chainState = new ChainState(log, contracts, handler, config.HistoryStartUtc);
        }

        public async Task<TimeSegmentResponse> OnNewSegment(TimeRange timeRange)
        {
            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var numberOfChainEvents = await ProcessEvents(timeRange);
                var duration = sw.Elapsed;

                if (numberOfChainEvents == 0) return TimeSegmentResponse.Underload;
                if (numberOfChainEvents > 10) return TimeSegmentResponse.Overload;
                if (duration > TimeSpan.FromSeconds(1)) return TimeSegmentResponse.Overload;
                return TimeSegmentResponse.OK;
            }
            catch (Exception ex)
            {
                var msg = "Exception processing time segment: " + ex;
                log.Error(msg); 
                eventsFormatter.OnError(msg);
                throw;
            }
        }

        private async Task<int> ProcessEvents(TimeRange timeRange)
        {
            var numberOfChainEvents = chainState.Update(timeRange.To);

            var events = eventsFormatter.GetEvents();
            var errors = eventsFormatter.GetErrors();

            var request = builder.Build(events, errors);
            if (request.HasAny())
            {
                await client.SendRewards(request);
            }
            return numberOfChainEvents;
        }
    }
}
