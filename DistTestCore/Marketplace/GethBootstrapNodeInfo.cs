using KubernetesWorkflow;

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
    }
}
