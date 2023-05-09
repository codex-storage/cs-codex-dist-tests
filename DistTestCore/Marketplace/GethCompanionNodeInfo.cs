using KubernetesWorkflow;
using Logging;
using NethereumWorkflow;

namespace DistTestCore.Marketplace
{
    public class GethCompanionNodeInfo
    {
        public GethCompanionNodeInfo(RunningContainer runningContainer, GethAccount[] accounts)
        {
            RunningContainer = runningContainer;
            Accounts = accounts;
        }

        public RunningContainer RunningContainer { get; }
        public GethAccount[] Accounts { get; }
        
        public NethereumInteraction StartInteraction(BaseLog log, GethAccount account)
        {
            var ip = RunningContainer.Pod.Cluster.IP;
            var port = RunningContainer.ServicePorts[0].Number;
            var privateKey = account.PrivateKey;

            var creator = new NethereumInteractionCreator(log, ip, port, privateKey);
            return creator.CreateWorkflow();
        }
    }

    public class GethAccount
    {
        public GethAccount(string account, string privateKey)
        {
            Account = account;
            PrivateKey = privateKey;
        }

        public string Account { get; }
        public string PrivateKey { get; }
    }
}
