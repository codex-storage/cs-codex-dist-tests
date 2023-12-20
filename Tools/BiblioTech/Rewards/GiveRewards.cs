namespace BiblioTech.Rewards
{
    public class GiveRewards
    {
        public Reward[] Rewards { get; set; } = Array.Empty<Reward>();
    }

    public class Reward
    {
        public ulong RewardId { get; set; }
        public string[] UserAddresses { get; set; } = Array.Empty<string>();
    }
}
