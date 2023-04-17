using DistTestCore.Marketplace;
using KubernetesWorkflow;

namespace DistTestCore
{
    public class GethStarter
    {
        private readonly GethBootstrapNodeCache bootstrapNodeCache;
        private readonly GethCompanionNodeStarter companionNodeStarter;
        private readonly TestLifecycle lifecycle;

        public GethStarter(TestLifecycle lifecycle, WorkflowCreator workflowCreator)
        {
            bootstrapNodeCache = new GethBootstrapNodeCache(new GethBootstrapNodeStarter(lifecycle, workflowCreator));
            companionNodeStarter = new GethCompanionNodeStarter(lifecycle, workflowCreator);
            this.lifecycle = lifecycle;
        }

        public GethStartResult BringOnlineMarketplaceFor(CodexSetup codexSetup)
        {
            if (codexSetup.MarketplaceConfig == null) return CreateMarketplaceUnavailableResult();

            var bootstrapNode = bootstrapNodeCache.Get();
            var companionNodes = StartCompanionNodes(codexSetup, bootstrapNode);

            TransferInitialBalance(bootstrapNode, codexSetup.MarketplaceConfig.InitialBalance, companionNodes);

            return CreateGethStartResult(bootstrapNode, companionNodes);
        }

        private void TransferInitialBalance(GethBootstrapNodeInfo bootstrapNode, int initialBalance, GethCompanionNodeInfo[] companionNodes)
        {
            var interaction = bootstrapNode.StartInteraction(lifecycle.Log);
            foreach (var node in companionNodes)
            {
                interaction.TransferTo(node.Account, initialBalance);
            }
        }

        private GethStartResult CreateGethStartResult(GethBootstrapNodeInfo bootstrapNode, GethCompanionNodeInfo[] companionNodes)
        {
            return new GethStartResult(CreateMarketplaceAccessFactory(bootstrapNode), bootstrapNode, companionNodes);
        }

        private GethStartResult CreateMarketplaceUnavailableResult()
        {
            return new GethStartResult(new MarketplaceUnavailableAccessFactory(), null!, Array.Empty<GethCompanionNodeInfo>());
        }

        private IMarketplaceAccessFactory CreateMarketplaceAccessFactory(GethBootstrapNodeInfo bootstrapNode)
        {
            return new GethMarketplaceAccessFactory(lifecycle.Log, bootstrapNode!);
        }

        private GethCompanionNodeInfo[] StartCompanionNodes(CodexSetup codexSetup, GethBootstrapNodeInfo bootstrapNode)
        {
            return companionNodeStarter.StartCompanionNodesFor(codexSetup, bootstrapNode);
        }
    }

    public class GethBootstrapNodeCache
    {
        private readonly GethBootstrapNodeStarter bootstrapNodeStarter;
        private GethBootstrapNodeInfo? bootstrapNode;

        public GethBootstrapNodeCache(GethBootstrapNodeStarter bootstrapNodeStarter)
        {
            this.bootstrapNodeStarter = bootstrapNodeStarter;
        }

        public GethBootstrapNodeInfo Get()
        {
            if (bootstrapNode == null)
            {
                bootstrapNode = bootstrapNodeStarter.StartGethBootstrapNode();
            }
            return bootstrapNode;
        }
    }
}
