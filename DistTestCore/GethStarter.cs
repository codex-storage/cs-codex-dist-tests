using DistTestCore.Marketplace;
using KubernetesWorkflow;

namespace DistTestCore
{
    public class GethStarter
    {
        private readonly TestLifecycle lifecycle;
        private readonly GethBootstrapNodeStarter bootstrapNodeStarter;
        private readonly GethCompanionNodeStarter companionNodeStarter;
        private GethBootstrapNodeInfo? bootstrapNode;

        public GethStarter(TestLifecycle lifecycle, WorkflowCreator workflowCreator)
        {
            this.lifecycle = lifecycle;

            bootstrapNodeStarter = new GethBootstrapNodeStarter(lifecycle, workflowCreator);
            companionNodeStarter = new GethCompanionNodeStarter(lifecycle, workflowCreator);
        }

        public GethStartResult BringOnlineMarketplaceFor(CodexSetup codexSetup)
        {
            if (codexSetup.MarketplaceConfig == null) return CreateMarketplaceUnavailableResult();

            EnsureBootstrapNode();
            var companionNodes = StartCompanionNodes(codexSetup);

            TransferInitialBalance(codexSetup.MarketplaceConfig.InitialBalance, bootstrapNode, companionNodes);

            return new GethStartResult(CreateMarketplaceAccessFactory(), bootstrapNode!, companionNodes);
        }

        private void TransferInitialBalance(int initialBalance, GethBootstrapNodeInfo? bootstrapNode, GethCompanionNodeInfo[] companionNodes)
        {
            aaaa
        }

        private GethStartResult CreateMarketplaceUnavailableResult()
        {
            return new GethStartResult(new MarketplaceUnavailableAccessFactory(), null!, Array.Empty<GethCompanionNodeInfo>());
        }

        private IMarketplaceAccessFactory CreateMarketplaceAccessFactory()
        {
            throw new NotImplementedException();
        }

        private void EnsureBootstrapNode()
        {
            if (bootstrapNode != null) return;
            bootstrapNode = bootstrapNodeStarter.StartGethBootstrapNode();
        }

        private GethCompanionNodeInfo[] StartCompanionNodes(CodexSetup codexSetup)
        {
            return companionNodeStarter.StartCompanionNodesFor(codexSetup, bootstrapNode!);
        }
    }
}
