using CodexContractsPlugin;
using CodexContractsPlugin.ChainMonitor;
using DiscordRewards;
using GethPlugin;
using Logging;
using Newtonsoft.Json;
using System.Numerics;
using Utils;

namespace TestNetRewarder
{
    public class Processor : ITimeSegmentHandler, IChainStateChangeHandler
    {
        private readonly RewardChecker rewardChecker = new RewardChecker();
        private readonly MarketTracker marketTracker = new MarketTracker();
        private readonly ChainState chainState;
        private readonly Configuration config;
        private readonly ILog log;
        private BlockInterval? lastBlockRange;

        public Processor(Configuration config, ICodexContracts contracts, ILog log)
        {
            this.config = config;
            this.log = log;

            chainState = new ChainState(log, contracts, this, config.HistoryStartUtc);
        }

        public async Task OnNewSegment(TimeRange timeRange)
        {
            try
            {
                chainState.Update(timeRange.To);
                
                await ProcessChainState(chainState);
            }
            catch (Exception ex)
            {
                log.Error("Exception processing time segment: " + ex);
                throw;
            }
        }

        private async Task ProcessChainState(ChainState chainState)
        {
            log.Log(chainState.EntireString());

            var outgoingRewards = new List<RewardUsersCommand>();
            foreach (var reward in rewardRepo.Rewards)
            {
                ProcessReward(outgoingRewards, reward, chainState);
            }

            var marketAverages = GetMarketAverages(chainState);
            var eventsOverview = GenerateEventsOverview(chainState);

            log.Log($"Found {outgoingRewards.Count} rewards. " +
                $"Found {marketAverages.Length} market averages. " +
                $"Found {eventsOverview.Length} events.");

            if (outgoingRewards.Any() || marketAverages.Any() || eventsOverview.Any())
            {
                if (!await SendRewardsCommand(outgoingRewards, marketAverages, eventsOverview))
                {
                    log.Error("Failed to send reward command.");
                }
            }
        }

        private string[] GenerateEventsOverview(ChainState chainState)
        {
            return chainState.GenerateOverview();
        }

        private MarketAverage[] GetMarketAverages(ChainState chainState)
        {
            return marketTracker.ProcessChainState(chainState);
        }

        public void OnNewRequest(IChainStateRequest request)
        {
            throw new NotImplementedException();
        }

        public void OnRequestStarted(IChainStateRequest request)
        {
            throw new NotImplementedException();
        }

        public void OnRequestFinished(IChainStateRequest request)
        {
            throw new NotImplementedException();
        }

        public void OnRequestFulfilled(IChainStateRequest request)
        {
            throw new NotImplementedException();
        }

        public void OnRequestCancelled(IChainStateRequest request)
        {
            throw new NotImplementedException();
        }

        public void OnSlotFilled(IChainStateRequest request, BigInteger slotIndex)
        {
            throw new NotImplementedException();
        }

        public void OnSlotFreed(IChainStateRequest request, BigInteger slotIndex)
        {
            throw new NotImplementedException();
        }
    }
}
