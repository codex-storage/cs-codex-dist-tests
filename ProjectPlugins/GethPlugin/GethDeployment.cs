using Core;
using KubernetesWorkflow;

namespace GethPlugin
{
    public class GethDeployment : IHasContainer
    {
        public GethDeployment(RunningContainer container, Port discoveryPort, Port httpPort, Port wsPort, AllGethAccounts allAccounts, string pubKey)
        {
            Container = container;
            DiscoveryPort = discoveryPort;
            HttpPort = httpPort;
            WsPort = wsPort;
            AllAccounts = allAccounts;
            PubKey = pubKey;
        }

        public RunningContainer Container { get; }
        public Port DiscoveryPort { get; }
        public Port HttpPort { get; }
        public Port WsPort { get; }
        public AllGethAccounts AllAccounts { get; }
        public string PubKey { get; }
    }
}
