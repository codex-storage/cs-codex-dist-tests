using CodexContractsPlugin.ChainMonitor;
using DiscordRewards;

namespace TestNetRewarder
{
    public class RewardChecker
    {
        public RewardChecker(IRewardGiver giver)
        {
            var repo = new RewardRepo();
            var checks = repo.Rewards.Select(r => new RewardCheck(r, giver)).ToArray();
            Handler = new ChainChangeMux(checks);
        }

        public IChainStateChangeHandler Handler { get; }
    }
}
