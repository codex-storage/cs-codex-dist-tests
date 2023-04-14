using KubernetesWorkflow;
using Logging;
using NethereumWorkflow;

namespace DistTestCore.Marketplace
{
    public class GethBootstrapNodeInfo
    {
        public GethBootstrapNodeInfo(RunningContainers runningContainers, string account, string genesisJsonBase64)
        {
            RunningContainers = runningContainers;
            Account = account;
            GenesisJsonBase64 = genesisJsonBase64;
        }

        public RunningContainers RunningContainers { get; }
        public string Account { get; }
        public string GenesisJsonBase64 { get; }

        public NethereumInteraction StartInteraction(TestLog log)
        {
            var ip = RunningContainers.RunningPod.Cluster.IP;
            var port = RunningContainers.Containers[0].ServicePorts[0].Number;

            var creator = new NethereumInteractionCreator(log, ip, port, Account);
            return creator.CreateWorkflow();
        }
    }
}
