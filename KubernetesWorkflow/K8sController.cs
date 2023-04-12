namespace KubernetesWorkflow
{
    public class K8sController
    {
        private readonly K8sCluster cluster;

        public K8sController(K8sCluster cluster)
        {
            this.cluster = cluster;
        }

        public RunningPod BringOnline(ContainerRecipe[] containerRecipes)
        {
            // Ensure namespace
            // create deployment
            // create service if necessary
            // wait until deployment online
            // fetch pod info

            // for each container, there is now an array of service ports available.

            return null!;
        }
    }
}
