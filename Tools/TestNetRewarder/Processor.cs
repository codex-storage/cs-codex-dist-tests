using DiscordRewards;
using GethPlugin;
using Logging;
using Newtonsoft.Json;
using Utils;

namespace TestNetRewarder
{
    public class Processor
    {
        private static readonly HistoricState historicState = new HistoricState();
        private static readonly RewardRepo rewardRepo = new RewardRepo();
        private static readonly MarketTracker marketTracker = new MarketTracker();
        private readonly ILog log;
        private BlockInterval? lastBlockRange;

        public Processor(ILog log)
        {
            this.log = log;
        }

        public async Task ProcessTimeSegment(TimeRange timeRange)
        {
            var connector = GethConnector.GethConnector.Initialize(log);
            if (connector == null) throw new Exception("Invalid Geth information");

            try
            {
                var blockRange = connector.GethNode.ConvertTimeRangeToBlockRange(timeRange);
                if (!IsNewBlockRange(blockRange))
                {
                    log.Log($"Block range {blockRange} was previously processed. Skipping...");
                    return;
                }

                var chainState = new ChainState(historicState, connector.CodexContracts, blockRange);
                await ProcessChainState(chainState);
            }
            catch (Exception ex)
            {
                log.Error("Exception processing time segment: " + ex);
                throw;
            }
        }

        private bool IsNewBlockRange(BlockInterval blockRange)
        {
            if (lastBlockRange == null ||
                lastBlockRange.From != blockRange.From || 
                lastBlockRange.To != blockRange.To)
            {
                lastBlockRange = blockRange;
                return true;
            }

            return false;
        }

        private async Task ProcessChainState(ChainState chainState)
        {
            var outgoingRewards = new List<RewardUsersCommand>();
            foreach (var reward in rewardRepo.Rewards)
            {
                ProcessReward(outgoingRewards, reward, chainState);
            }

            var marketAverages = GetMarketAverages(chainState);

            log.Log($"Found {outgoingRewards.Count} rewards to send. Found {marketAverages.Length} market averages.");

            if (outgoingRewards.Any())
            {
                if (!await SendRewardsCommand(outgoingRewards, marketAverages))
                {
                    log.Error("Failed to send reward command.");
                }
            }
        }

        private MarketAverage[] GetMarketAverages(ChainState chainState)
        {
            return marketTracker.ProcessChainState(chainState);
        }

        private async Task<bool> SendRewardsCommand(List<RewardUsersCommand> outgoingRewards, MarketAverage[] marketAverages)
        {
            var cmd = new GiveRewardsCommand
            {
                Rewards = outgoingRewards.ToArray(),
                Averages = marketAverages.ToArray()
            };

            log.Debug("Sending rewards: " + JsonConvert.SerializeObject(cmd));
            return await Program.BotClient.SendRewards(cmd);
        }

        private void ProcessReward(List<RewardUsersCommand> outgoingRewards, RewardConfig reward, ChainState chainState)
        {
            var winningAddresses = PerformCheck(reward, chainState);
            foreach (var win in winningAddresses)
            {
                log.Log($"Address '{win.Address}' wins '{reward.Message}'");
            }
            if (winningAddresses.Any())
            {
                outgoingRewards.Add(new RewardUsersCommand
                {
                    RewardId = reward.RoleId,
                    UserAddresses = winningAddresses.Select(a => a.Address).ToArray()
                });
            }
        }

        private EthAddress[] PerformCheck(RewardConfig reward, ChainState chainState)
        {
            var check = GetCheck(reward.CheckConfig);
            return check.Check(chainState).Distinct().ToArray();
        }

        private ICheck GetCheck(CheckConfig config)
        {
            switch (config.Type)
            {
                case CheckType.FilledSlot:
                    return new FilledAnySlotCheck();
                case CheckType.FinishedSlot:
                    return new FinishedSlotCheck(config.MinSlotSize, config.MinDuration);
                case CheckType.PostedContract:
                    return new PostedContractCheck(config.MinNumberOfHosts, config.MinSlotSize, config.MinDuration);
                case CheckType.StartedContract:
                    return new StartedContractCheck(config.MinNumberOfHosts, config.MinSlotSize, config.MinDuration);
            }

            throw new Exception("Unknown check type: " + config.Type);
        }
    }
}
