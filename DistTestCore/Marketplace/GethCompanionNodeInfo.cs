using KubernetesWorkflow;

namespace DistTestCore.Marketplace
{
    public class GethCompanionNodeInfo
    {
        public GethCompanionNodeInfo(RunningContainer runningContainer, string account)
        {
            RunningContainer = runningContainer;
            Account = account;
        }

        public RunningContainer RunningContainer { get; }
        public string Account { get; }
    }
}
