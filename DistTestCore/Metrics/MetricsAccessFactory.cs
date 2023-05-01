using KubernetesWorkflow;

namespace DistTestCore.Metrics
{
    public interface IMetricsAccessFactory
    {
        IMetricsAccess CreateMetricsAccess(RunningContainer codexContainer);
    }

    public class MetricsUnavailableAccessFactory : IMetricsAccessFactory
    {
        public IMetricsAccess CreateMetricsAccess(RunningContainer codexContainer)
        {
            return new MetricsUnavailable();
        }
    }

    public class CodexNodeMetricsAccessFactory : IMetricsAccessFactory
    {
        private readonly TestLifecycle lifecycle;
        private readonly RunningContainers prometheusContainer;

        public CodexNodeMetricsAccessFactory(TestLifecycle lifecycle, RunningContainers prometheusContainer)
        {
            this.lifecycle = lifecycle;
            this.prometheusContainer = prometheusContainer;
        }

        public IMetricsAccess CreateMetricsAccess(RunningContainer codexContainer)
        {
            var query = new MetricsQuery(lifecycle.Log, prometheusContainer);
            return new MetricsAccess(lifecycle.Log, query, codexContainer);
        }
    }
}
