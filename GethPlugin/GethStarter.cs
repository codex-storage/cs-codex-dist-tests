using Core;
using KubernetesWorkflow;

namespace GethPlugin
{
    public class GethStarter
    {
        private readonly IPluginTools tools;

        public GethStarter(IPluginTools tools)
        {
            this.tools = tools;
        }

        public IGethNodeInfo StartGeth(GethStartupConfig gethStartupConfig)
        {
            Log("Starting Geth bootstrap node...");

            var startupConfig = new StartupConfig();
            startupConfig.Add(gethStartupConfig);
            startupConfig.NameOverride = gethStartupConfig.NameOverride;

            var workflow = tools.CreateWorkflow();
            var containers = workflow.Start(1, Location.Unspecified, new GethContainerRecipe(), startupConfig);
            if (containers.Containers.Length != 1) throw new InvalidOperationException("Expected 1 Geth bootstrap node to be created. Test infra failure.");
            var container = containers.Containers[0];

            var extractor = new GethContainerInfoExtractor(tools.GetLog(), workflow, container);
            var accounts = extractor.ExtractAccounts();
            var pubKey = extractor.ExtractPubKey();
            
            var discoveryPort = container.Recipe.GetPortByTag(GethContainerRecipe.DiscoveryPortTag);
            if (discoveryPort == null) throw new Exception("Expected discovery port to be created.");
            var httpPort = container.Recipe.GetPortByTag(GethContainerRecipe.HttpPortTag);
            if (httpPort == null) throw new Exception("Expected http port to be created.");
            var wsPort = container.Recipe.GetPortByTag(GethContainerRecipe.wsPortTag);
            if (wsPort == null) throw new Exception("Expected ws port to be created.");

            var result = new GethNodeInfo(container, accounts, pubKey, discoveryPort, httpPort, wsPort);

            Log($"Geth bootstrap node started with account '{result.Account.Account}'");

            return result;
        }

        private void Log(string msg)
        {
            tools.GetLog().Log(msg);
        }

        //public GethStartResult BringOnlineMarketplaceFor(CodexSetup codexSetup)
        //{
        //    if (codexSetup.MarketplaceConfig == null) return CreateMarketplaceUnavailableResult();

        //    var marketplaceNetwork = marketplaceNetworkCache.Get();
        //    var companionNode = StartCompanionNode(codexSetup, marketplaceNetwork);

        //    LogStart("Setting up initial balance...");
        //    TransferInitialBalance(marketplaceNetwork, codexSetup.MarketplaceConfig, companionNode);
        //    LogEnd($"Initial balance of {codexSetup.MarketplaceConfig.InitialTestTokens} set for {codexSetup.NumberOfNodes} nodes.");

        //    return CreateGethStartResult(marketplaceNetwork, companionNode);
        //}

        //private void TransferInitialBalance(MarketplaceNetwork marketplaceNetwork, MarketplaceInitialConfig marketplaceConfig, GethCompanionNodeInfo companionNode)
        //{
        //    if (marketplaceConfig.InitialTestTokens.Amount == 0) return;

        //    var interaction = marketplaceNetwork.StartInteraction(lifecycle);
        //    var tokenAddress = marketplaceNetwork.Marketplace.TokenAddress;

        //    var accounts = companionNode.Accounts.Select(a => a.Account).ToArray();
        //    interaction.MintTestTokens(accounts, marketplaceConfig.InitialTestTokens.Amount, tokenAddress);
        //}

        //private GethStartResult CreateGethStartResult(MarketplaceNetwork marketplaceNetwork, GethCompanionNodeInfo companionNode)
        //{
        //    return new GethStartResult(CreateMarketplaceAccessFactory(marketplaceNetwork), marketplaceNetwork, companionNode);
        //}

        //private GethStartResult CreateMarketplaceUnavailableResult()
        //{
        //    return new GethStartResult(new MarketplaceUnavailableAccessFactory(), null!, null!);
        //}

        //private IMarketplaceAccessFactory CreateMarketplaceAccessFactory(MarketplaceNetwork marketplaceNetwork)
        //{
        //    return new GethMarketplaceAccessFactory(lifecycle, marketplaceNetwork);
        //}

        //private GethCompanionNodeInfo StartCompanionNode(CodexSetup codexSetup, MarketplaceNetwork marketplaceNetwork)
        //{
        //    return companionNodeStarter.StartCompanionNodeFor(codexSetup, marketplaceNetwork);
        //}
    }

    //public class MarketplaceNetworkCache
    //{
    //    private readonly GethBootstrapNodeStarter bootstrapNodeStarter;
    //    private readonly CodexContractsStarter codexContractsStarter;
    //    private MarketplaceNetwork? network;

    //    public MarketplaceNetworkCache(GethBootstrapNodeStarter bootstrapNodeStarter, CodexContractsStarter codexContractsStarter)
    //    {
    //        this.bootstrapNodeStarter = bootstrapNodeStarter;
    //        this.codexContractsStarter = codexContractsStarter;
    //    }

    //    public MarketplaceNetwork Get()
    //    {
    //        if (network == null)
    //        {
    //            var bootstrapInfo = bootstrapNodeStarter.StartGethBootstrapNode();
    //            var marketplaceInfo = codexContractsStarter.Start(bootstrapInfo);
    //            network = new MarketplaceNetwork(bootstrapInfo, marketplaceInfo);
    //        }
    //        return network;
    //    }
    //}
}
