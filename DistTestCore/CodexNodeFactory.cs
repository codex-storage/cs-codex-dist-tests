using DistTestCore.Codex;
using DistTestCore.Marketplace;
using DistTestCore.Metrics;

namespace DistTestCore
{
    public interface ICodexNodeFactory
    {
        OnlineCodexNode CreateOnlineCodexNode(CodexAccess access, CodexNodeGroup group);
    }

    public class CodexNodeFactory : ICodexNodeFactory
    {
        private readonly TestLifecycle lifecycle;
        private readonly IMetricsAccessFactory metricsAccessFactory;
        private readonly IMarketplaceAccessFactory marketplaceAccessFactory;

        public CodexNodeFactory(TestLifecycle lifecycle, IMetricsAccessFactory metricsAccessFactory, IMarketplaceAccessFactory marketplaceAccessFactory)
        {
            this.lifecycle = lifecycle;
            this.metricsAccessFactory = metricsAccessFactory;
            this.marketplaceAccessFactory = marketplaceAccessFactory;
        }

        public OnlineCodexNode CreateOnlineCodexNode(CodexAccess access, CodexNodeGroup group)
        {
            var metricsAccess = metricsAccessFactory.CreateMetricsAccess(access.Container);
            var marketplaceAccess = marketplaceAccessFactory.CreateMarketplaceAccess(access);
            return new OnlineCodexNode(lifecycle, access, group, metricsAccess, marketplaceAccess);
        }
    }
}
