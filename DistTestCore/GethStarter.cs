using DistTestCore.Marketplace;
using KubernetesWorkflow;

namespace DistTestCore
{
    public class GethStarter
    {
        private readonly TestLifecycle lifecycle;
        private readonly WorkflowCreator workflowCreator;
        private readonly GethBootstrapNodeStarter bootstrapNodeStarter;
        private GethBootstrapNodeInfo? bootstrapNode;

        public GethStarter(TestLifecycle lifecycle, WorkflowCreator workflowCreator)
        {
            this.lifecycle = lifecycle;
            this.workflowCreator = workflowCreator;

            bootstrapNodeStarter = new GethBootstrapNodeStarter(lifecycle, workflowCreator);
        }

        public object BringOnlineMarketplaceFor(CodexSetup codexSetup)
        {
            EnsureBootstrapNode();
            StartCompanionNodes(codexSetup);
            return null!;
        }

        private void EnsureBootstrapNode()
        {
            if (bootstrapNode != null) return;
            bootstrapNode = bootstrapNodeStarter.StartGethBootstrapNode();
        }

        private void StartCompanionNodes(CodexSetup codexSetup)
        {
            throw new NotImplementedException();
        }

        private void Log(string msg)
        {
            lifecycle.Log.Log(msg);
        }
    }
}
