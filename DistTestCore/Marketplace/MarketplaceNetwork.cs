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

        public NethereumInteraction StartInteraction(TestLog log)
        {
            var ip = Bootstrap.RunningContainers.RunningPod.Cluster.IP;
            var port = Bootstrap.RunningContainers.Containers[0].ServicePorts[0].Number;
            var account = Bootstrap.Account;
            var privateKey = Bootstrap.PrivateKey;

            var creator = new NethereumInteractionCreator(log, ip, port, account, privateKey);
            return creator.CreateWorkflow();
        }
    }
}
