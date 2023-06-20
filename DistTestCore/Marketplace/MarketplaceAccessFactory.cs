using DistTestCore.Codex;

namespace DistTestCore.Marketplace
{
    public interface IMarketplaceAccessFactory
    {
        IMarketplaceAccess CreateMarketplaceAccess(CodexAccess access);
    }

    public class MarketplaceUnavailableAccessFactory : IMarketplaceAccessFactory
    {
        public IMarketplaceAccess CreateMarketplaceAccess(CodexAccess access)
        {
            return new MarketplaceUnavailable();
        }
    }

    public class GethMarketplaceAccessFactory : IMarketplaceAccessFactory
    {
        private readonly TestLifecycle lifecycle;
        private readonly MarketplaceNetwork marketplaceNetwork;

        public GethMarketplaceAccessFactory(TestLifecycle lifecycle, MarketplaceNetwork marketplaceNetwork)
        {
            this.lifecycle = lifecycle;
            this.marketplaceNetwork = marketplaceNetwork;
        }

        public IMarketplaceAccess CreateMarketplaceAccess(CodexAccess access)
        {
            var companionNode = GetGethCompanionNode(access);
            return new MarketplaceAccess(lifecycle, marketplaceNetwork, companionNode, access);
        }

        private GethAccount GetGethCompanionNode(CodexAccess access)
        {
            var account = access.Container.Recipe.Additionals.Single(a => a is GethAccount);
            return (GethAccount)account;
        }
    }
}
