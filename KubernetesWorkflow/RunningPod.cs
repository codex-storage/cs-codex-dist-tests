namespace KubernetesWorkflow
{
    public class RunningPod
    {
        private readonly Dictionary<ContainerRecipe, Port[]> servicePortMap;

        public RunningPod(K8sCluster cluster, string name, string ip, Dictionary<ContainerRecipe, Port[]> servicePortMap)
        {
            Cluster = cluster;
            Name = name;
            Ip = ip;
            this.servicePortMap = servicePortMap;
        }

        public K8sCluster Cluster { get; }
        public string Name { get; }
        public string Ip { get; }

        public Port[] GetServicePortsForContainerRecipe(ContainerRecipe containerRecipe)
        {
            return servicePortMap[containerRecipe];
        }
    }
}
