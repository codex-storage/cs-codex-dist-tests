namespace DistTestCore.Marketplace
{
    public class GethStartResult
    {
        public GethStartResult(IMarketplaceAccessFactory marketplaceAccessFactory, MarketplaceNetwork marketplaceNetwork, GethCompanionNodeInfo[] companionNodes)
        {
            MarketplaceAccessFactory = marketplaceAccessFactory;
            MarketplaceNetwork = marketplaceNetwork;
            CompanionNodes = companionNodes;
        }

        public IMarketplaceAccessFactory MarketplaceAccessFactory { get; }
        public MarketplaceNetwork MarketplaceNetwork { get; }
        public GethCompanionNodeInfo[] CompanionNodes { get; }
    }
}
