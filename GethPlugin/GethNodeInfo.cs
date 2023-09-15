using KubernetesWorkflow;

namespace GethPlugin
{
    public interface IGethNodeInfo
    {
    }

    public class GethNodeInfo : IGethNodeInfo
    {
        public GethNodeInfo(RunningContainer runningContainer, AllGethAccounts allAccounts, string pubKey, Port discoveryPort)
        {
            RunningContainer = runningContainer;
            AllAccounts = allAccounts;
            Account = allAccounts.Accounts[0];
            PubKey = pubKey;
            DiscoveryPort = discoveryPort;
        }

        public RunningContainer RunningContainer { get; }
        public AllGethAccounts AllAccounts { get; }
        public GethAccount Account { get; }
        public string PubKey { get; }
        public Port DiscoveryPort { get; }

        //public NethereumInteraction StartInteraction(TestLifecycle lifecycle)
        //{
        //    var address = lifecycle.Configuration.GetAddress(RunningContainers.Containers[0]);
        //    var account = Account;

        //    var creator = new NethereumInteractionCreator(lifecycle.Log, address.Host, address.Port, account.PrivateKey);
        //    return creator.CreateWorkflow();
        //}
    }
}
