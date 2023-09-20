using KubernetesWorkflow;

namespace GethPlugin
{
    public interface IGethDeployment
    {
        RunningContainer RunningContainer { get; }
        Port DiscoveryPort { get; }
        Port HttpPort { get; }
        Port WsPort { get; }
        AllGethAccounts AllAccounts { get; }
        string PubKey { get; }
    }

    public class GethDeployment : IGethDeployment
    {
        public GethDeployment(RunningContainer runningContainer, Port discoveryPort, Port httpPort, Port wsPort, AllGethAccounts allAccounts, string pubKey)
        {
            RunningContainer = runningContainer;
            DiscoveryPort = discoveryPort;
            HttpPort = httpPort;
            WsPort = wsPort;
            AllAccounts = allAccounts;
            PubKey = pubKey;
        }

        public RunningContainer RunningContainer { get; }
        public Port DiscoveryPort { get; }
        public Port HttpPort { get; }
        public Port WsPort { get; }
        public AllGethAccounts AllAccounts { get; }
        public string PubKey { get; }
    }
}
