using DistTestCore.Codex;
using Logging;

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
        private readonly TestLog log;
        private readonly GethBootstrapNodeInfo bootstrapNode;

        public GethMarketplaceAccessFactory(TestLog log, GethBootstrapNodeInfo bootstrapNode)
        {
            this.log = log;
            this.bootstrapNode = bootstrapNode;
        }

        public IMarketplaceAccess CreateMarketplaceAccess(CodexAccess access)
        {
            var companionNode = GetGethCompanionNode(access);
            return new MarketplaceAccess(log, bootstrapNode, companionNode);
        }

        private GethCompanionNodeInfo GetGethCompanionNode(CodexAccess access)
        {
            var node = access.Container.Recipe.Additionals.Single(a => a is GethCompanionNodeInfo);
            return (GethCompanionNodeInfo)node;
        }
    }
}
