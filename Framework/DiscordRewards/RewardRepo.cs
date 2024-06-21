using Utils;

namespace DiscordRewards
{
    public class RewardRepo
    {
        private static string Tag => RewardConfig.UsernameTag;

        public RewardConfig[] Rewards { get; } = new RewardConfig[]
        {
            // Filled any slot
            new RewardConfig(1187039439558541498, $"{Tag} successfully filled their first slot!", new CheckConfig
            {
                Type = CheckType.HostFilledSlot
            }),

            // Finished any slot
            new RewardConfig(1202286165630390339, $"{Tag} successfully finished their first slot!", new CheckConfig
            {
                Type = CheckType.HostFinishedSlot
            }),

            // Finished a sizable slot
            new RewardConfig(1202286218738405418, $"{Tag} finished their first 1GB-24h slot! (10mb/5mins for test)", new CheckConfig
            {
                Type = CheckType.HostFinishedSlot,
                MinSlotSize = 10.MB(),
                MinDuration = TimeSpan.FromMinutes(5.0),
            }),

            // Posted any contract
            new RewardConfig(1202286258370383913, $"{Tag} posted their first contract!", new CheckConfig
            {
                Type = CheckType.ClientPostedContract
            }),

            // Started any contract
            new RewardConfig(1202286330873126992, $"A contract created by {Tag} reached Started state for the first time!", new CheckConfig
            {
                Type = CheckType.ClientStartedContract
            }),

            // Started a sizable contract
            new RewardConfig(1202286381670608909, $"A large contract created by {Tag} reached Started state for the first time! (10mb/5mins for test)", new CheckConfig
            {
                Type = CheckType.ClientStartedContract,
                MinNumberOfHosts = 4,
                MinSlotSize = 10.MB(),
                MinDuration = TimeSpan.FromMinutes(5.0),
            })
        };
    }
}
