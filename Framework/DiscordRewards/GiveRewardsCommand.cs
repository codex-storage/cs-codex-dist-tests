namespace DiscordRewards
{
    public class GiveRewardsCommand
    {
        public RewardUsersCommand[] Rewards { get; set; } = Array.Empty<RewardUsersCommand>();
        public MarketAverage[] Averages { get; set; } = Array.Empty<MarketAverage>();
        public string[] EventsOverview { get; set; } = Array.Empty<string>();

        public bool HasAny()
        {
            return Rewards.Any() || Averages.Any() || EventsOverview.Any();
        }
    }

    public class RewardUsersCommand
    {
        public ulong RewardId { get; set; }
        public string[] UserAddresses { get; set; } = Array.Empty<string>();
    }

    public class MarketAverage
    {
        public int NumberOfFinished { get; set; }
        public int TimeRangeSeconds { get; set; }
        public float Price { get; set; }
        public float Size { get; set; }
        public float Duration { get; set; }
        public float Collateral { get; set; }
        public float ProofProbability { get; set; }
    }
}
