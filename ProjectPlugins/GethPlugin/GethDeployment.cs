using Core;
using KubernetesWorkflow;

namespace GethPlugin
{
    public class GethDeployment : IHasContainer
    {
        public GethDeployment(RunningContainer container, Port discoveryPort, Port httpPort, Port wsPort, GethAccount account, string pubKey)
        {
            Container = container;
            DiscoveryPort = discoveryPort;
            HttpPort = httpPort;
            WsPort = wsPort;
            Account = account;
            PubKey = pubKey;
        }

        public RunningContainer Container { get; }
        public Port DiscoveryPort { get; }
        public Port HttpPort { get; }
        public Port WsPort { get; }
        public GethAccount Account { get; }
        public string PubKey { get; }
    }
}
