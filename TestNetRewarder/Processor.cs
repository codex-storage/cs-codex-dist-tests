using BiblioTech.Rewards;
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
                await SendRewardsCommand(outgoingRewards);
            }
        }

        private async Task SendRewardsCommand(List<RewardUsersCommand> outgoingRewards)
        {
            var cmd = new GiveRewardsCommand
            {
                Rewards = outgoingRewards.ToArray()
            };

            log.Debug("Sending rewards: " + JsonConvert.SerializeObject(cmd));
            await Program.BotClient.SendRewards(cmd);
        }

        private void ProcessReward(List<RewardUsersCommand> outgoingRewards, RewardConfig reward, ChainState chainState)
        {
            var winningAddresses = reward.Check.Check(chainState);
            if (winningAddresses.Any())
            {
                outgoingRewards.Add(new RewardUsersCommand
                {
                    RewardId = reward.RewardId,
                    UserAddresses = winningAddresses.Select(a => a.Address).ToArray()
                });
            }
        }
    }
}
