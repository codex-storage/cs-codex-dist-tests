namespace DiscordRewards
{
    public class GiveRewardsCommand
    {
        public RewardUsersCommand[] Rewards { get; set; } = Array.Empty<RewardUsersCommand>();
        public ChainEventMessage[] EventsOverview { get; set; } = Array.Empty<ChainEventMessage>();
        public string[] Errors { get; set; } = Array.Empty<string>();

        public bool HasAny()
        {
            return Rewards.Any() || EventsOverview.Any();
        }
    }

    public class RewardUsersCommand
    {
        public ulong RewardId { get; set; }
        public string[] UserAddresses { get; set; } = Array.Empty<string>();
    }

    public class ChainEventMessage
    {
        public ulong BlockNumber { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
