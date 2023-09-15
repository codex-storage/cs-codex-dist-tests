using KubernetesWorkflow;
using NethereumWorkflow;

namespace GethPlugin
{
    public class GethBootstrapNodeInfo
    {
        public GethBootstrapNodeInfo(RunningContainers runningContainers, AllGethAccounts allAccounts, string pubKey, Port discoveryPort)
        {
            RunningContainers = runningContainers;
            AllAccounts = allAccounts;
            Account = allAccounts.Accounts[0];
            PubKey = pubKey;
            DiscoveryPort = discoveryPort;
        }

        public RunningContainers RunningContainers { get; }
        public AllGethAccounts AllAccounts { get; }
        public GethAccount Account { get; }
        public string PubKey { get; }
        public Port DiscoveryPort { get; }

        public NethereumInteraction StartInteraction(TestLifecycle lifecycle)
        {
            var address = lifecycle.Configuration.GetAddress(RunningContainers.Containers[0]);
            var account = Account;

            var creator = new NethereumInteractionCreator(lifecycle.Log, address.Host, address.Port, account.PrivateKey);
            return creator.CreateWorkflow();
        }
    }

    public class AllGethAccounts
    {
        public GethAccount[] Accounts { get; }

        public AllGethAccounts(GethAccount[] accounts)
        {
            Accounts = accounts;
        }
    }
}
