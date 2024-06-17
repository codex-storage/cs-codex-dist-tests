using CodexContractsPlugin.ChainMonitor;
using DiscordRewards;
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

        public void OnNewRequest(IChainStateRequest request)
        {
        }

        public void OnRequestFinished(IChainStateRequest request)
        {
            foreach (var b in buffers) b.Add(request);
        }

        public void OnRequestFulfilled(IChainStateRequest request)
        {
        }

        public void OnRequestCancelled(IChainStateRequest request)
        {
        }

        public void OnSlotFilled(IChainStateRequest request, BigInteger slotIndex)
        {
        }

        public void OnSlotFreed(IChainStateRequest request, BigInteger slotIndex)
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
    }
}
