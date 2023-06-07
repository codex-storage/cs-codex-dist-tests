using KubernetesWorkflow;
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
        
        public NethereumInteraction StartInteraction(TestLifecycle lifecycle, GethAccount account)
        {
            var address = lifecycle.Configuration.GetAddress(RunningContainer);
            var privateKey = account.PrivateKey;

            var creator = new NethereumInteractionCreator(lifecycle.Log, address.Host, address.Port, privateKey);
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
