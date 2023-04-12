using Utils;

namespace KubernetesWorkflow
{
    public class WorkflowCreator
    {
        private readonly NumberSource numberSource = new NumberSource(0);
        private readonly NumberSource servicePortNumberSource = new NumberSource(30001);
        private readonly K8sCluster cluster = new K8sCluster();
        private readonly KnownK8sPods knownPods = new KnownK8sPods();

        public StartupWorkflow CreateWorkflow()
        {
            var workflowNumberSource = new WorkflowNumberSource(numberSource.GetNextNumber(), servicePortNumberSource);

            return new StartupWorkflow(workflowNumberSource, cluster, knownPods);
        }
    }
}
