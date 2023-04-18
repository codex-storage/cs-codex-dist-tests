using KubernetesWorkflow;

namespace DistTestCore.Marketplace
{
    public class GethBootstrapNodeInfo
    {
        public GethBootstrapNodeInfo(RunningContainers runningContainers, string account, string genesisJsonBase64, string pubKey, Port discoveryPort)
        {
            RunningContainers = runningContainers;
            Account = account;
            GenesisJsonBase64 = genesisJsonBase64;
            PubKey = pubKey;
            DiscoveryPort = discoveryPort;
        }

        public RunningContainers RunningContainers { get; }
        public string Account { get; }
        public string GenesisJsonBase64 { get; }
        public string PubKey { get; }
        public Port DiscoveryPort { get; }
    }
}
