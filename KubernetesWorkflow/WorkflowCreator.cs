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
        private readonly ILog log;
        private readonly string k8sNamespace;

        public WorkflowCreator(ILog log, Configuration configuration)
        {
            this.log = log;

            cluster = new K8sCluster(configuration);
            k8sNamespace = configuration.KubernetesNamespace.ToLowerInvariant();
        }

        public IStartupWorkflow CreateWorkflow()
        {
            var workflowNumberSource = new WorkflowNumberSource(numberSource.GetNextNumber(),
                                                                    containerNumberSource);

            return new StartupWorkflow(log, workflowNumberSource, cluster, knownPods, k8sNamespace);
        }
    }
}
