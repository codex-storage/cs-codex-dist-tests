using DistTestCore;

namespace CodexPlugin
{
    public interface ICodexNodeFactory
    {
        OnlineCodexNode CreateOnlineCodexNode(CodexAccess access, CodexNodeGroup group);
    }

    public class CodexNodeFactory : ICodexNodeFactory
    {
        private readonly IPluginTools tools;

        public CodexNodeFactory(IPluginTools tools)
        {
            this.tools = tools;
        }

        //private readonly TestLifecycle lifecycle;
        //private readonly IMetricsAccessFactory metricsAccessFactory;
        //private readonly IMarketplaceAccessFactory marketplaceAccessFactory;

        //public CodexNodeFactory(TestLifecycle lifecycle, IMetricsAccessFactory metricsAccessFactory, IMarketplaceAccessFactory marketplaceAccessFactory)
        //{
        //    this.lifecycle = lifecycle;
        //    this.metricsAccessFactory = metricsAccessFactory;
        //    this.marketplaceAccessFactory = marketplaceAccessFactory;
        //}

        public OnlineCodexNode CreateOnlineCodexNode(CodexAccess access, CodexNodeGroup group)
        {
            //var metricsAccess = metricsAccessFactory.CreateMetricsAccess(access.Container);
            //var marketplaceAccess = marketplaceAccessFactory.CreateMarketplaceAccess(access);
            return new OnlineCodexNode(tools, access, group/*, metricsAccess, marketplaceAccess*/);
        }
    }
}
