using Utils;

namespace TestNetRewarder
{
    public class RewardConfig
    {
        public RewardConfig(ulong rewardId, ICheck check)
        {
            RewardId = rewardId;
            Check = check;
        }

        public ulong RewardId { get; }
        public ICheck Check { get; }
    }

    public class RewardRepo
    {
        public RewardConfig[] Rewards { get; } = new RewardConfig[]
        {
            // Filled any slot
            new RewardConfig(123, new FilledAnySlotCheck()),

            // Finished any slot
            new RewardConfig(124, new FinishedSlotCheck(
                minSize: 0.Bytes(),
                minDuration: TimeSpan.Zero)),

            // Finished a sizable slot
            new RewardConfig(125, new FinishedSlotCheck(
                minSize: 1.GB(),
                minDuration: TimeSpan.FromHours(24.0))),

            // Posted any contract
            new RewardConfig(126, new PostedContractCheck()),

            // Started any contract
            new RewardConfig(127, new StartedContractCheck(
                minNumberOfHosts: 1,
                minSlotSize: 0.Bytes(),
                minDuration: TimeSpan.Zero)),
            
            // Started a sizable contract
            new RewardConfig(127, new StartedContractCheck(
                minNumberOfHosts: 4,
                minSlotSize: 1.GB(),
                minDuration: TimeSpan.FromHours(24.0)))
        };
    }
}
