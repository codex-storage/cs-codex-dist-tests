namespace DistTestCore.Marketplace
{
    public class GethStartResult
    {
        public GethStartResult(IMarketplaceAccessFactory marketplaceAccessFactory, GethBootstrapNodeInfo bootstrapNode, GethCompanionNodeInfo[] companionNodes)
        {
            MarketplaceAccessFactory = marketplaceAccessFactory;
            BootstrapNode = bootstrapNode;
            CompanionNodes = companionNodes;
        }

        public IMarketplaceAccessFactory MarketplaceAccessFactory { get; }
        public GethBootstrapNodeInfo BootstrapNode { get; }
        public GethCompanionNodeInfo[] CompanionNodes { get; }
    }
}
