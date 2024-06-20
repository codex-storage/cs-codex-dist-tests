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
        private readonly BufferLogger bufferLogger;
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
            bufferLogger = new BufferLogger();

            var handler = new ChainChangeMux(
                rewardChecker.Handler,
                marketTracker
            );

            chainState = new ChainState(new LogSplitter(log, bufferLogger), contracts, handler, config.HistoryStartUtc);
        }

        public async Task OnNewSegment(TimeRange timeRange)
        {
            try
            {
                chainState.Update(timeRange.To);

                var averages = marketTracker.GetAverages();
                var lines = RemoveFirstLine(bufferLogger.Get());

                var request = builder.Build(averages, lines);
                if (request.HasAny())
                {
                    await client.SendRewards(request);
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception processing time segment: " + ex);
                throw;
            }
        }

        private string[] RemoveFirstLine(string[] lines)
        {
            if (!lines.Any()) return Array.Empty<string>();
            return lines.Skip(1).ToArray();
        }
    }
}
