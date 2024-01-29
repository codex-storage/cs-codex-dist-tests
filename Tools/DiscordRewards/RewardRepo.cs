using Utils;

namespace DiscordRewards
{
    public class RewardRepo
    {
        private static string Tag => RewardConfig.UsernameTag;

        public RewardConfig[] Rewards { get; } = new RewardConfig[]
        {
            // Filled any slot
            new RewardConfig(123, "Filled a slot", new CheckConfig
            {
                Type = CheckType.FilledSlot
            }),

            // Finished any slot
            new RewardConfig(124, "Finished any slot", new CheckConfig
            {
                Type = CheckType.FinishedSlot
            }),

            // Finished a sizable slot
            new RewardConfig(125, "Finished sizable slot", new CheckConfig
            {
                Type = CheckType.FinishedSlot,
                MinSlotSize = 1.GB(),
                MinDuration = TimeSpan.FromHours(24.0),
            }),

            // Posted any contract
            new RewardConfig(126, "Posted any contract", new CheckConfig
            {
                Type = CheckType.PostedContract
            }),

            // Started any contract
            new RewardConfig(127, "Started any contract", new CheckConfig
            {
                Type = CheckType.StartedContract
            }),

            // Started a sizable contract
            new RewardConfig(125, "Started sizable contract", new CheckConfig
            {
                Type = CheckType.FinishedSlot,
                MinNumberOfHosts = 4,
                MinSlotSize = 1.GB(),
                MinDuration = TimeSpan.FromHours(24.0),
            })
        };
    }
}
