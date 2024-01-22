namespace BiblioTech.Rewards
{
    public class RewardsRepo
    {
        private static string Tag => RoleController.UsernameTag;

        public RoleRewardConfig[] Rewards { get; }

        public RewardsRepo()
        {
            Rewards = new[]
            {
                // Join reward
                new RoleRewardConfig(1187039439558541498, $"Congratulations {Tag}, you got the test-reward!"),

                //// Hosting:
                //// Filled any slot:
                //new RoleRewardConfig(1187039439558541498, $"Congratulations {Tag}, you got the test-reward!"),

                //// Finished any slot:
                //new RoleRewardConfig(1187039439558541498, $"Congratulations {Tag}, you got the test-reward!"),

                //// Finished a min-256MB min-8h slot:
                //new RoleRewardConfig(1187039439558541498, $"Congratulations {Tag}, you got the test-reward!"),

                //// Finished a min-64GB  min-24h slot:
                //new RoleRewardConfig(1187039439558541498, $"Congratulations {Tag}, you got the test-reward!"),

                //// Oops:
                //// Missed a storage proof:
                //new RoleRewardConfig(1187039439558541498, $"Congratulations {Tag}, you got the test-reward!"),

                //// Clienting:
                //// Posted any contract:
                //new RoleRewardConfig(1187039439558541498, $"Congratulations {Tag}, you got the test-reward!"),

                //// Posted any contract that reached started state:
                //new RoleRewardConfig(1187039439558541498, $"Congratulations {Tag}, you got the test-reward!"),

                //// Started a contract with min-4 hosts, min-256MB per host, min-8h duration:
                //new RoleRewardConfig(1187039439558541498, $"Congratulations {Tag}, you got the test-reward!"),

                //// Started a contract with min-4 hosts, min-64GB per host, min-24h duration:
                //new RoleRewardConfig(1187039439558541498, $"Congratulations {Tag}, you got the test-reward!"),
            };
        }
    }
}
