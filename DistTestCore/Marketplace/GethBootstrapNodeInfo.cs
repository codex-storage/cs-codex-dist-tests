using KubernetesWorkflow;
using Logging;
using NethereumWorkflow;

namespace DistTestCore.Marketplace
{
    public class GethBootstrapNodeInfo
    {
        public GethBootstrapNodeInfo(RunningContainers runningContainers, string account, string pubKey, string privateKey, Port discoveryPort)
        {
            RunningContainers = runningContainers;
            Account = account;
            PubKey = pubKey;
            PrivateKey = privateKey;
            DiscoveryPort = discoveryPort;
        }

        public RunningContainers RunningContainers { get; }
        public string Account { get; }
        public string PubKey { get; }
        public string PrivateKey { get; }
        public Port DiscoveryPort { get; }

        public NethereumInteraction StartInteraction(TestLog log)
        {
            var ip = RunningContainers.RunningPod.Cluster.IP;
            var port = RunningContainers.Containers[0].ServicePorts[0].Number;
            var account = Account;
            var privateKey = PrivateKey;

            var creator = new NethereumInteractionCreator(log, ip, port, account, privateKey);
            return creator.CreateWorkflow();
        }
    }
}
