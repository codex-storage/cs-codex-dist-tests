using CodexContractsPlugin;
using CodexContractsPlugin.ChainMonitor;
using Logging;
using Utils;

namespace TestNetRewarder
{
    public class Processor : ITimeSegmentHandler
    {
        private readonly RequestBuilder builder;
        private readonly EventsFormatter eventsFormatter;
        private readonly ChainState chainState;
        private readonly Configuration config;
        private readonly BotClient client;
        private readonly ILog log;
        private DateTime lastPeriodUpdateUtc;

        public Processor(Configuration config, BotClient client, ICodexContracts contracts, ILog log)
        {
            this.config = config;
            this.client = client;
            this.log = log;
            lastPeriodUpdateUtc = DateTime.UtcNow;

            builder = new RequestBuilder();
            eventsFormatter = new EventsFormatter(config);

            chainState = new ChainState(log, contracts, eventsFormatter, config.HistoryStartUtc,
                doProofPeriodMonitoring: config.ShowProofPeriodReports > 0);
        }

        public async Task Initialize()
        {
            var events = eventsFormatter.GetInitializationEvents(config);
            var request = builder.Build(events, Array.Empty<string>());
            if (request.HasAny())
            {
                await client.SendRewards(request);
            }
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
            ProcessPeriodUpdate();

            var events = eventsFormatter.GetEvents();
            var errors = eventsFormatter.GetErrors();

            var request = builder.Build(events, errors);
            if (request.HasAny())
            {
                await client.SendRewards(request);
            }
            return numberOfChainEvents;
        }

        private void ProcessPeriodUpdate()
        {
            if (config.ShowProofPeriodReports < 1) return;
            if (DateTime.UtcNow < (lastPeriodUpdateUtc + TimeSpan.FromHours(1.0))) return;
            lastPeriodUpdateUtc = DateTime.UtcNow;

            eventsFormatter.ProcessPeriodReports(chainState.PeriodMonitor.GetAndClearReports());
        }
    }
}
