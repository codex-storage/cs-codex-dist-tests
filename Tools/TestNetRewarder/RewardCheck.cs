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

        public void OnNewRequest(RequestEvent requestEvent)
        {
            if (MeetsRequirements(CheckType.ClientPostedContract, requestEvent))
            {
                GiveReward(reward, requestEvent.Request.Client);
            }
        }

        public void OnRequestCancelled(RequestEvent requestEvent)
        {
        }

        public void OnRequestFailed(RequestEvent requestEvent)
        {
        }

        public void OnRequestFinished(RequestEvent requestEvent)
        {
            if (MeetsRequirements(CheckType.HostFinishedSlot, requestEvent))
            {
                foreach (var host in requestEvent.Request.Hosts.GetHosts())
                {
                    GiveReward(reward, host);
                }
            }
        }

        public void OnRequestFulfilled(RequestEvent requestEvent)
        {
            if (MeetsRequirements(CheckType.ClientStartedContract, requestEvent))
            {
                GiveReward(reward, requestEvent.Request.Client);
            }
        }

        public void OnSlotFilled(RequestEvent requestEvent, EthAddress host, BigInteger slotIndex)
        {
            if (MeetsRequirements(CheckType.HostFilledSlot, requestEvent))
            {
                if (host != null)
                {
                    GiveReward(reward, host);
                }
            }
        }

        public void OnSlotFreed(RequestEvent requestEvent, BigInteger slotIndex)
        {
        }

        public void OnSlotReservationsFull(RequestEvent requestEvent, BigInteger slotIndex)
        {
        }

        public void OnError(string msg)
        {
        }

        private void GiveReward(RewardConfig reward, EthAddress receiver)
        {
            giver.Give(reward, receiver);
        }

        private bool MeetsRequirements(CheckType type, RequestEvent requestEvent)
        {
            return
                reward.CheckConfig.Type == type &&
                MeetsDurationRequirement(requestEvent.Request) &&
                MeetsSizeRequirement(requestEvent.Request);
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
