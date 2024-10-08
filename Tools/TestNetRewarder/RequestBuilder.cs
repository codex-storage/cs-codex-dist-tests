﻿using DiscordRewards;
using GethPlugin;

namespace TestNetRewarder
{
    public class RequestBuilder : IRewardGiver
    {
        private readonly Dictionary<ulong, List<EthAddress>> rewards = new Dictionary<ulong, List<EthAddress>>();

        public void Give(RewardConfig reward, EthAddress receiver)
        {
            if (rewards.ContainsKey(reward.RoleId))
            {
                rewards[reward.RoleId].Add(receiver);
            }
            else
            {
                rewards.Add(reward.RoleId, new List<EthAddress> { receiver });
            }
        }

        public GiveRewardsCommand Build(string[] lines)
        {
            var result = new GiveRewardsCommand
            {
                Rewards = rewards.Select(p => new RewardUsersCommand
                {
                    RewardId = p.Key,
                    UserAddresses = p.Value.Select(v => v.Address).ToArray()
                }).ToArray(),
                EventsOverview = lines
            };

            rewards.Clear();

            return result;
        }
    }
}
