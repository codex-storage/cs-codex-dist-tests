using Logging;
using Utils;

namespace KubernetesWorkflow
{
    public class WorkflowCreator
    {
        private readonly NumberSource numberSource = new NumberSource(0);
        private readonly NumberSource containerNumberSource = new NumberSource(0);
        private readonly K8sCluster cluster;
        private readonly ILog log;
        private readonly Configuration configuration;
        private readonly string k8sNamespace;

        public WorkflowCreator(ILog log, Configuration configuration)
        {
            this.log = log;
            this.configuration = configuration;
            cluster = new K8sCluster(configuration);
            k8sNamespace = configuration.KubernetesNamespace.ToLowerInvariant();
        }

        public IStartupWorkflow CreateWorkflow(string? namespaceOverride = null)
        {
            var workflowNumberSource = new WorkflowNumberSource(numberSource.GetNextNumber(),
                                                                    containerNumberSource);

            return new StartupWorkflow(log, workflowNumberSource, cluster, GetNamespace(namespaceOverride), configuration.Replacer);
        }

        private string GetNamespace(string? namespaceOverride)
        {
            if (namespaceOverride != null)
            {
                if (!configuration.AllowNamespaceOverride) throw new Exception("Namespace override is not allowed.");
                return namespaceOverride;
            }
            return k8sNamespace;
        }
    }
}
