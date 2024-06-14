using CodexContractsPlugin.ChainMonitor;
using DiscordRewards;
using GethPlugin;
using Nethereum.Model;
using Newtonsoft.Json;
using System.Numerics;

namespace TestNetRewarder
{
    public class RewardChecker : IChainStateChangeHandler
    {
        private static readonly RewardRepo rewardRepo = new RewardRepo();

        private async Task<bool> SendRewardsCommand(List<RewardUsersCommand> outgoingRewards, MarketAverage[] marketAverages, string[] eventsOverview)
        {
            var cmd = new GiveRewardsCommand
            {
                Rewards = outgoingRewards.ToArray(),
                Averages = marketAverages.ToArray(),
                EventsOverview = eventsOverview
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
