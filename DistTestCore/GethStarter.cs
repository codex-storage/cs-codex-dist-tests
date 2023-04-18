using DistTestCore.Marketplace;
using KubernetesWorkflow;

namespace DistTestCore
{
    public class GethStarter // basestarter
    {
        private readonly MarketplaceNetworkCache marketplaceNetworkCache;
        private readonly GethCompanionNodeStarter companionNodeStarter;
        private readonly TestLifecycle lifecycle;

        public GethStarter(TestLifecycle lifecycle, WorkflowCreator workflowCreator)
        {
            marketplaceNetworkCache = new MarketplaceNetworkCache(
                new GethBootstrapNodeStarter(lifecycle, workflowCreator),
                new CodexContractsStarter(lifecycle, workflowCreator));
            companionNodeStarter = new GethCompanionNodeStarter(lifecycle, workflowCreator);
            this.lifecycle = lifecycle;
        }

        public GethStartResult BringOnlineMarketplaceFor(CodexSetup codexSetup)
        {
            if (codexSetup.MarketplaceConfig == null) return CreateMarketplaceUnavailableResult();

            var marketplaceNetwork = marketplaceNetworkCache.Get();
            var companionNodes = StartCompanionNodes(codexSetup, marketplaceNetwork);

            TransferInitialBalance(marketplaceNetwork, codexSetup.MarketplaceConfig, companionNodes);

            return CreateGethStartResult(marketplaceNetwork, companionNodes);
        }

        private void TransferInitialBalance(MarketplaceNetwork marketplaceNetwork, MarketplaceInitialConfig marketplaceConfig, GethCompanionNodeInfo[] companionNodes)
        {
            var interaction = marketplaceNetwork.StartInteraction(lifecycle.Log);
            foreach (var node in companionNodes)
            {
                interaction.TransferTo(node.Account, marketplaceConfig.InitialEth.Wei);

                var tokenAddress = interaction.GetTokenAddress(marketplaceNetwork.Marketplace.Address);

                interaction.MintTestTokens(node.Account, marketplaceConfig.InitialTestTokens.Amount, tokenAddress);
            }
        }

        private GethStartResult CreateGethStartResult(MarketplaceNetwork marketplaceNetwork, GethCompanionNodeInfo[] companionNodes)
        {
            return new GethStartResult(CreateMarketplaceAccessFactory(marketplaceNetwork), marketplaceNetwork, companionNodes);
        }

        private GethStartResult CreateMarketplaceUnavailableResult()
        {
            return new GethStartResult(new MarketplaceUnavailableAccessFactory(), null!, Array.Empty<GethCompanionNodeInfo>());
        }

        private IMarketplaceAccessFactory CreateMarketplaceAccessFactory(MarketplaceNetwork marketplaceNetwork)
        {
            return new GethMarketplaceAccessFactory(lifecycle.Log, marketplaceNetwork);
        }

        private GethCompanionNodeInfo[] StartCompanionNodes(CodexSetup codexSetup, MarketplaceNetwork marketplaceNetwork)
        {
            return companionNodeStarter.StartCompanionNodesFor(codexSetup, marketplaceNetwork.Bootstrap);
        }
    }

    public class MarketplaceNetworkCache
    {
        private readonly GethBootstrapNodeStarter bootstrapNodeStarter;
        private readonly CodexContractsStarter codexContractsStarter;
        private MarketplaceNetwork? network;

        public MarketplaceNetworkCache(GethBootstrapNodeStarter bootstrapNodeStarter, CodexContractsStarter codexContractsStarter)
        {
            this.bootstrapNodeStarter = bootstrapNodeStarter;
            this.codexContractsStarter = codexContractsStarter;
        }

        public MarketplaceNetwork Get()
        {
            if (network == null)
            {
                var bootstrapInfo = bootstrapNodeStarter.StartGethBootstrapNode();
                var marketplaceInfo = codexContractsStarter.Start(bootstrapInfo.RunningContainers.Containers[0]);
                network = new MarketplaceNetwork(bootstrapInfo, marketplaceInfo );
            }
            return network;
        }
    }
}
