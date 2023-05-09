using KubernetesWorkflow;
using Logging;
using NethereumWorkflow;

namespace DistTestCore.Marketplace
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

        public NethereumInteraction StartInteraction(BaseLog log)
        {
            var ip = RunningContainers.RunningPod.Cluster.IP;
            var port = RunningContainers.Containers[0].ServicePorts[0].Number;
            var account = Account;

            var creator = new NethereumInteractionCreator(log, ip, port, account.PrivateKey);
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
