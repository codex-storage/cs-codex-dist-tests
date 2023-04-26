using KubernetesWorkflow;
using Logging;
using NethereumWorkflow;

namespace DistTestCore.Marketplace
{
    public class GethCompanionNodeInfo
    {
        public GethCompanionNodeInfo(RunningContainer runningContainer, GethCompanionAccount[] accounts)
        {
            RunningContainer = runningContainer;
            Accounts = accounts;
        }

        public RunningContainer RunningContainer { get; }
        public GethCompanionAccount[] Accounts { get; }
        
        public NethereumInteraction StartInteraction(BaseLog log, GethCompanionAccount account)
        {
            var ip = RunningContainer.Pod.Cluster.IP;
            var port = RunningContainer.ServicePorts[0].Number;
            var accountStr = account.Account;
            var privateKey = account.PrivateKey;

            var creator = new NethereumInteractionCreator(log, ip, port, accountStr, privateKey);
            return creator.CreateWorkflow();
        }
    }

    public class GethCompanionAccount
    {
        public GethCompanionAccount(string account, string privateKey)
        {
            Account = account;
            PrivateKey = privateKey;
        }

        public string Account { get; }
        public string PrivateKey { get; }
    }
}
