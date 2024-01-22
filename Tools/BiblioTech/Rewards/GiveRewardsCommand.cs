using Newtonsoft.Json;

namespace BiblioTech.Rewards
{
    public class GiveRewardsCommand
    {
        public RewardUsersCommand[] Rewards { get; set; } = Array.Empty<RewardUsersCommand>();
    }

    public class RewardUsersCommand
    {
        public ulong RewardId { get; set; }
        public string[] UserAddresses { get; set; } = Array.Empty<string>();

        [JsonIgnore]
        public UserData[] Users { get; set; } = Array.Empty<UserData>();
    }
}
