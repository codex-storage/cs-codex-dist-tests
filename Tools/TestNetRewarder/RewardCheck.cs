using CodexContractsPlugin.ChainMonitor;
using DiscordRewards;
using GethPlugin;
using NethereumWorkflow;
using System.Numerics;

namespace TestNetRewarder
{
    public interface IRewardGiver
    {
        void Give(RewardConfig reward, EthAddress receiver);
    }

    public class RewardCheck : IChainStateChangeHandler
    {
        private readonly RewardConfig reward;
        private readonly IRewardGiver giver;

        public RewardCheck(RewardConfig reward, IRewardGiver giver)
        {
            this.reward = reward;
            this.giver = giver;
        }

        public void OnNewRequest(IChainStateRequest request)
        {
            if (MeetsRequirements(CheckType.ClientPostedContract, request))
            {
                GiveReward(reward, request.Client);
            }
        }

        public void OnRequestCancelled(IChainStateRequest request)
        {
        }

        public void OnRequestFinished(IChainStateRequest request)
        {
            if (MeetsRequirements(CheckType.HostFinishedSlot, request))
            {
                foreach (var host in request.Hosts.GetHosts())
                {
                    GiveReward(reward, host);
                }
            }
        }

        public void OnRequestFulfilled(IChainStateRequest request)
        {
            if (MeetsRequirements(CheckType.ClientStartedContract, request))
            {
                GiveReward(reward, request.Client);
            }
        }

        public void OnSlotFilled(IChainStateRequest request, BigInteger slotIndex)
        {
            if (MeetsRequirements(CheckType.HostFilledSlot, request))
            {
                var host = request.Hosts.GetHost((int)slotIndex);
                if (host != null)
                {
                    GiveReward(reward, host);
                }
            }
        }

        public void OnSlotFreed(IChainStateRequest request, BigInteger slotIndex)
        {
        }

        private void GiveReward(RewardConfig reward, EthAddress receiver)
        {
            giver.Give(reward, receiver);
        }

        private bool MeetsRequirements(CheckType type, IChainStateRequest request)
        {
            return
                reward.CheckConfig.Type == type &&
                MeetsDurationRequirement(request) &&
                MeetsSizeRequirement(request);
        }

        private bool MeetsSizeRequirement(IChainStateRequest r)
        {
            var slotSize = r.Request.Ask.SlotSize.ToDecimal();
            decimal min = reward.CheckConfig.MinSlotSize.SizeInBytes;
            return slotSize >= min;
        }

        private bool MeetsDurationRequirement(IChainStateRequest r)
        {
            var duration = TimeSpan.FromSeconds((double)r.Request.Ask.Duration);
            return duration >= reward.CheckConfig.MinDuration;
        }
    }
}
