namespace DiscordRewards
{
    public class GiveRewardsCommand
    {
        public RewardUsersCommand[] Rewards { get; set; } = Array.Empty<RewardUsersCommand>();
    }

    public class RewardUsersCommand
    {
        public ulong RewardId { get; set; }
        public string[] UserAddresses { get; set; } = Array.Empty<string>();
    }
}
