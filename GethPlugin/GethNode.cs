using KubernetesWorkflow;
using Logging;
using NethereumWorkflow;

namespace GethPlugin
{
    public interface IGethNode
    {
        RunningContainer RunningContainer { get; }
        Port DiscoveryPort { get; }
        Port HttpPort { get; }
        Port WsPort { get; }

        NethereumInteraction StartInteraction(ILog log);
    }

    public class GethNode : IGethNode
    {
        public GethNode(RunningContainer runningContainer, AllGethAccounts allAccounts, string pubKey, Port discoveryPort, Port httpPort, Port wsPort)
        {
            RunningContainer = runningContainer;
            AllAccounts = allAccounts;
            Account = allAccounts.Accounts[0];
            PubKey = pubKey;
            DiscoveryPort = discoveryPort;
            HttpPort = httpPort;
            WsPort = wsPort;
        }

        public RunningContainer RunningContainer { get; }
        public AllGethAccounts AllAccounts { get; }
        public GethAccount Account { get; }
        public string PubKey { get; }
        public Port DiscoveryPort { get; }
        public Port HttpPort { get; }
        public Port WsPort { get; }

        public NethereumInteraction StartInteraction(ILog log)
        {
            var address = RunningContainer.Address;
            var account = Account;

            var creator = new NethereumInteractionCreator(log, address.Host, address.Port, account.PrivateKey);
            return creator.CreateWorkflow();
        }
    }
}
