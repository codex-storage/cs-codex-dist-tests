using DistTestCore.Codex;
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

        public CodexNodeFactory(TestLifecycle lifecycle, IMetricsAccessFactory metricsAccessFactory)
        {
            this.lifecycle = lifecycle;
            this.metricsAccessFactory = metricsAccessFactory;
        }

        public OnlineCodexNode CreateOnlineCodexNode(CodexAccess access, CodexNodeGroup group)
        {
            var metricsAccess = metricsAccessFactory.CreateMetricsAccess(access.Container);
            return new OnlineCodexNode(lifecycle, access, group, metricsAccess);
        }
    }
}
