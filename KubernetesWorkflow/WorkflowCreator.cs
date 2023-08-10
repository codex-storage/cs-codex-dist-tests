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
        private readonly PodLabels podLabels;
        private readonly string testNamespace;

        public WorkflowCreator(BaseLog log, Configuration configuration, PodLabels podLabels)
        {
            cluster = new K8sCluster(configuration);
            this.log = log;
            this.podLabels = podLabels;
            testNamespace = Guid.NewGuid().ToString().ToLowerInvariant();
        }

        public StartupWorkflow CreateWorkflow()
        {
            var workflowNumberSource = new WorkflowNumberSource(numberSource.GetNextNumber(),
                                                                    containerNumberSource);

            return new StartupWorkflow(log, workflowNumberSource, cluster, knownPods, testNamespace, podLabels);
        }
    }
}
