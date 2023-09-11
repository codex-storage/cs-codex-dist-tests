//using DistTestCore.Marketplace;

//namespace CodexPlugin
//{
//    public class GethStarter : BaseStarter
//    {
//        private readonly MarketplaceNetworkCache marketplaceNetworkCache;
//        private readonly GethCompanionNodeStarter companionNodeStarter;

//        public GethStarter(TestLifecycle lifecycle)
//            : base(lifecycle)
//        {
//            marketplaceNetworkCache = new MarketplaceNetworkCache(
//                new GethBootstrapNodeStarter(lifecycle),
//                new CodexContractsStarter(lifecycle));
//            companionNodeStarter = new GethCompanionNodeStarter(lifecycle);
//        }

//        public GethStartResult BringOnlineMarketplaceFor(CodexSetup codexSetup)
//        {
//            if (codexSetup.MarketplaceConfig == null) return CreateMarketplaceUnavailableResult();

//            var marketplaceNetwork = marketplaceNetworkCache.Get();
//            var companionNode = StartCompanionNode(codexSetup, marketplaceNetwork);

//            LogStart("Setting up initial balance...");
//            TransferInitialBalance(marketplaceNetwork, codexSetup.MarketplaceConfig, companionNode);
//            LogEnd($"Initial balance of {codexSetup.MarketplaceConfig.InitialTestTokens} set for {codexSetup.NumberOfNodes} nodes.");

//            return CreateGethStartResult(marketplaceNetwork, companionNode);
//        }

//        private void TransferInitialBalance(MarketplaceNetwork marketplaceNetwork, MarketplaceInitialConfig marketplaceConfig, GethCompanionNodeInfo companionNode)
//        {
//            if (marketplaceConfig.InitialTestTokens.Amount == 0) return;

//            var interaction = marketplaceNetwork.StartInteraction(lifecycle);
//            var tokenAddress = marketplaceNetwork.Marketplace.TokenAddress;

//            var accounts = companionNode.Accounts.Select(a => a.Account).ToArray();
//            interaction.MintTestTokens(accounts, marketplaceConfig.InitialTestTokens.Amount, tokenAddress);
//        }

//        private GethStartResult CreateGethStartResult(MarketplaceNetwork marketplaceNetwork, GethCompanionNodeInfo companionNode)
//        {
//            return new GethStartResult(CreateMarketplaceAccessFactory(marketplaceNetwork), marketplaceNetwork, companionNode);
//        }

//        private GethStartResult CreateMarketplaceUnavailableResult()
//        {
//            return new GethStartResult(new MarketplaceUnavailableAccessFactory(), null!, null!);
//        }

//        private IMarketplaceAccessFactory CreateMarketplaceAccessFactory(MarketplaceNetwork marketplaceNetwork)
//        {
//            return new GethMarketplaceAccessFactory(lifecycle, marketplaceNetwork);
//        }

//        private GethCompanionNodeInfo StartCompanionNode(CodexSetup codexSetup, MarketplaceNetwork marketplaceNetwork)
//        {
//            return companionNodeStarter.StartCompanionNodeFor(codexSetup, marketplaceNetwork);
//        }
//    }

//    public class MarketplaceNetworkCache
//    {
//        private readonly GethBootstrapNodeStarter bootstrapNodeStarter;
//        private readonly CodexContractsStarter codexContractsStarter;
//        private MarketplaceNetwork? network;

//        public MarketplaceNetworkCache(GethBootstrapNodeStarter bootstrapNodeStarter, CodexContractsStarter codexContractsStarter)
//        {
//            this.bootstrapNodeStarter = bootstrapNodeStarter;
//            this.codexContractsStarter = codexContractsStarter;
//        }

//        public MarketplaceNetwork Get()
//        {
//            if (network == null)
//            {
//                var bootstrapInfo = bootstrapNodeStarter.StartGethBootstrapNode();
//                var marketplaceInfo = codexContractsStarter.Start(bootstrapInfo);
//                network = new MarketplaceNetwork(bootstrapInfo, marketplaceInfo );
//            }
//            return network;
//        }
//    }
//}
