using CodexContractsPlugin.ChainMonitor;
using DiscordRewards;
using GethPlugin;
using Logging;
using System.Numerics;

namespace TestNetRewarder
{
    public class MarketTracker : IChainStateChangeHandler
    {
        private readonly List<MarketBuffer> buffers = new List<MarketBuffer>();
        private readonly ILog log;

        public MarketTracker(Configuration config, ILog log)
        {
            var intervals = GetInsightCounts(config);

            foreach (var i in intervals)
            {
                buffers.Add(new MarketBuffer(
                    config.Interval * i
                ));
            }

            this.log = log;
        }

        public MarketAverage[] GetAverages()
        {
            foreach (var b in buffers) b.Update();

            return buffers.Select(b => b.GetAverage()).Where(a => a != null).Cast<MarketAverage>().ToArray();
        }

        public void OnNewRequest(RequestEvent requestEvent)
        {
        }

        public void OnRequestFinished(RequestEvent requestEvent)
        {
            foreach (var b in buffers) b.Add(requestEvent);
        }

        public void OnRequestFulfilled(RequestEvent requestEvent)
        {
        }

        public void OnRequestCancelled(RequestEvent requestEvent)
        {
        }

        public void OnSlotFilled(RequestEvent requestEvent, EthAddress host, BigInteger slotIndex)
        {
        }

        public void OnSlotFreed(RequestEvent requestEvent, BigInteger slotIndex)
        {
        }

        private int[] GetInsightCounts(Configuration config)
        {
            try
            {
                var tokens = config.MarketInsights.Split(';').ToArray();
                return tokens.Select(t => Convert.ToInt32(t)).ToArray();
            }
            catch (Exception ex)
            {
                log.Error($"Exception when parsing MarketInsights config parameters: {ex}");
            }
            return Array.Empty<int>();
        }

        public void OnRequestFailed(RequestEvent requestEvent)
        {
            throw new NotImplementedException("being removed");
        }
    }
}
