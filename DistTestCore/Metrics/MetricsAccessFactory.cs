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
        private readonly RunningContainers prometheusContainer;

        public CodexNodeMetricsAccessFactory(RunningContainers prometheusContainer)
        {
            this.prometheusContainer = prometheusContainer;
        }

        public IMetricsAccess CreateMetricsAccess(RunningContainer codexContainer)
        {
            var query = new MetricsQuery(prometheusContainer);
            return new MetricsAccess(query, codexContainer);
        }
    }
}
