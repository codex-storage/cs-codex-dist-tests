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
        private readonly ILog log;

        public Processor(ILog log)
        {
            this.log = log;
        }

        public async Task ProcessTimeSegment(TimeRange range)
        {
            try
            {
                var connector = GethConnector.GethConnector.Initialize(log);
                if (connector == null) return;

                var chainState = new ChainState(historicState, connector.CodexContracts, range);
                await ProcessTimeSegment(chainState);

            }
            catch (Exception ex)
            {
                log.Error("Exception processing time segment: " + ex);
            }
        }

        private async Task ProcessTimeSegment(ChainState chainState)
        {
            var outgoingRewards = new List<RewardUsersCommand>();
            foreach (var reward in rewardRepo.Rewards)
            {
                ProcessReward(outgoingRewards, reward, chainState);
            }

            if (outgoingRewards.Any())
            {
                if (!await SendRewardsCommand(outgoingRewards))
                {
                    log.Error("Failed to send reward command.");
                }
            }
        }

        private async Task<bool> SendRewardsCommand(List<RewardUsersCommand> outgoingRewards)
        {
            var cmd = new GiveRewardsCommand
            {
                Rewards = outgoingRewards.ToArray()
            };

            log.Debug("Sending rewards: " + JsonConvert.SerializeObject(cmd));
            return await Program.BotClient.SendRewards(cmd);
        }

        private void ProcessReward(List<RewardUsersCommand> outgoingRewards, RewardConfig reward, ChainState chainState)
        {
            var winningAddresses = PerformCheck(reward, chainState);
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
            return check.Check(chainState);
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
