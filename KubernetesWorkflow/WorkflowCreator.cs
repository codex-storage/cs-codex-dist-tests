namespace KubernetesWorkflow
{
    public class WorkflowCreator
    {
        private readonly NumberSource containerNumberSource = new NumberSource(0);
        private readonly K8sController controller = new K8sController(new K8sCluster());

        public StartupWorkflow CreateWorkflow()
        {
            return new StartupWorkflow(containerNumberSource, controller);
        }
    }
}
