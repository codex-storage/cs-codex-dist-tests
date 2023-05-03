using Logging;
using Utils;

namespace KubernetesWorkflow
{
    public class WorkflowCreator
    {
        private readonly NumberSource numberSource = new NumberSource(0);
        private readonly NumberSource containerNumberSource = new NumberSource(0);
        private readonly KnownK8sPods knownPods = new KnownK8sPods();
        private readonly K8sCluster cluster;
        private readonly BaseLog log;

        public WorkflowCreator(BaseLog log, Configuration configuration)
        {
            cluster = new K8sCluster(configuration);
            this.log = log;
        }

        public StartupWorkflow CreateWorkflow()
        {
            var workflowNumberSource = new WorkflowNumberSource(numberSource.GetNextNumber(),
                                                                    ApplicationLifecycle.Instance.GetServiceNumberSource(),
                                                                    containerNumberSource);

            return new StartupWorkflow(log, workflowNumberSource, cluster, knownPods, ApplicationLifecycle.Instance.GetTestNamespace());
        }
    }
}
