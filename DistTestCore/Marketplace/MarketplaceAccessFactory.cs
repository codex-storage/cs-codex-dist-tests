namespace DistTestCore.Marketplace
{
    public interface IMarketplaceAccessFactory
    {
        IMarketplaceAccess CreateMarketplaceAccess();
    }

    public class MarketplaceUnavailableAccessFactory : IMarketplaceAccessFactory
    {
        public IMarketplaceAccess CreateMarketplaceAccess()
        {
            return new MarketplaceUnavailable();
        }
    }

    public class GethMarketplaceAccessFactory : IMarketplaceAccessFactory
    {
        public IMarketplaceAccess CreateMarketplaceAccess()
        {
            
            return new MarketplaceAccess(query, codexContainer);
        }
    }
}
