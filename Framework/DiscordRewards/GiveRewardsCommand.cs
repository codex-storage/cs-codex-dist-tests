namespace DiscordRewards
{
    public class GiveRewardsCommand
    {
        public RewardUsersCommand[] Rewards { get; set; } = Array.Empty<RewardUsersCommand>();
        public MarketAverage[] Averages { get; set; } = Array.Empty<MarketAverage>();
        public string[] EventsOverview { get; set; } = Array.Empty<string>();
    }

    public class RewardUsersCommand
    {
        public ulong RewardId { get; set; }
        public string[] UserAddresses { get; set; } = Array.Empty<string>();
    }

    public class MarketAverage
    {
        public string Title { get; set; } = string.Empty;
        public float Price { get; set; }
        public float Size { get; set; }
        public float Duration { get; set; }
        public float Collateral { get; set; }
        public float ProofProbability { get; set; }
    }
}
