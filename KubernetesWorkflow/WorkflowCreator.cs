using Utils;

namespace KubernetesWorkflow
{
    public class WorkflowCreator
    {
        private readonly NumberSource numberSource = new NumberSource(0);
        private readonly NumberSource servicePortNumberSource = new NumberSource(30001);
        private readonly NumberSource containerNumberSource = new NumberSource(0);
        private readonly KnownK8sPods knownPods = new KnownK8sPods();
        private readonly K8sCluster cluster;

        public WorkflowCreator(Configuration configuration)
        {
            cluster = new K8sCluster(configuration);
        }

        public StartupWorkflow CreateWorkflow()
        {
            var workflowNumberSource = new WorkflowNumberSource(numberSource.GetNextNumber(),
                                                                    servicePortNumberSource,
                                                                    containerNumberSource);

            return new StartupWorkflow(workflowNumberSource, cluster, knownPods);
        }
    }
}
