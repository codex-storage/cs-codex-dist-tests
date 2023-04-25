using DistTestCore.Marketplace;
using KubernetesWorkflow;

namespace DistTestCore
{
    public class GethStarter : BaseStarter
    {
        private readonly MarketplaceNetworkCache marketplaceNetworkCache;
        private readonly GethCompanionNodeStarter companionNodeStarter;

        public GethStarter(TestLifecycle lifecycle, WorkflowCreator workflowCreator)
            : base(lifecycle, workflowCreator)
        {
            marketplaceNetworkCache = new MarketplaceNetworkCache(
                new GethBootstrapNodeStarter(lifecycle, workflowCreator),
                new CodexContractsStarter(lifecycle, workflowCreator));
            companionNodeStarter = new GethCompanionNodeStarter(lifecycle, workflowCreator);
        }

        public GethStartResult BringOnlineMarketplaceFor(CodexSetup codexSetup)
        {
            if (codexSetup.MarketplaceConfig == null) return CreateMarketplaceUnavailableResult();

            var marketplaceNetwork = marketplaceNetworkCache.Get();
            var companionNodes = StartCompanionNodes(codexSetup, marketplaceNetwork);

            LogStart("Setting up initial balance...");
            TransferInitialBalance(marketplaceNetwork, codexSetup.MarketplaceConfig, companionNodes);
            LogEnd($"Initial balance of {codexSetup.MarketplaceConfig.InitialTestTokens} set for {codexSetup.NumberOfNodes} nodes.");

            return CreateGethStartResult(marketplaceNetwork, companionNodes);
        }

        private void TransferInitialBalance(MarketplaceNetwork marketplaceNetwork, MarketplaceInitialConfig marketplaceConfig, GethCompanionNodeInfo[] companionNodes)
        {
            var interaction = marketplaceNetwork.StartInteraction(lifecycle.Log);
            var tokenAddress = marketplaceNetwork.Marketplace.TokenAddress;

            foreach (var node in companionNodes)
            {
                interaction.TransferWeiTo(node.Account, marketplaceConfig.InitialEth.Wei);
                interaction.MintTestTokens(node.Account, marketplaceConfig.InitialTestTokens.Amount, tokenAddress);
            }

            interaction.WaitForAllTransactions();
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
            return companionNodeStarter.StartCompanionNodesFor(codexSetup, marketplaceNetwork);
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
                var marketplaceInfo = codexContractsStarter.Start(bootstrapInfo);
                network = new MarketplaceNetwork(bootstrapInfo, marketplaceInfo );
            }
            return network;
        }
    }
}
