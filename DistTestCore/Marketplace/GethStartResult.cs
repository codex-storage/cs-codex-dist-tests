using Newtonsoft.Json;

namespace DistTestCore.Marketplace
{
    public class GethStartResult
    {
        public GethStartResult(IMarketplaceAccessFactory marketplaceAccessFactory, MarketplaceNetwork marketplaceNetwork, GethCompanionNodeInfo companionNode)
        {
            MarketplaceAccessFactory = marketplaceAccessFactory;
            MarketplaceNetwork = marketplaceNetwork;
            CompanionNode = companionNode;
        }

        [JsonIgnore]
        public IMarketplaceAccessFactory MarketplaceAccessFactory { get; }
        public MarketplaceNetwork MarketplaceNetwork { get; }
        public GethCompanionNodeInfo CompanionNode { get; }
    }
}
