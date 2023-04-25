using Logging;
using NethereumWorkflow;

namespace DistTestCore.Marketplace
{
    public class MarketplaceNetwork
    {
        public MarketplaceNetwork(GethBootstrapNodeInfo bootstrap, MarketplaceInfo marketplace)
        {
            Bootstrap = bootstrap;
            Marketplace = marketplace;
        }

        public GethBootstrapNodeInfo Bootstrap { get; }
        public MarketplaceInfo Marketplace { get; }

        public NethereumInteraction StartInteraction(BaseLog log)
        {
            return Bootstrap.StartInteraction(log);
        }
    }
}
