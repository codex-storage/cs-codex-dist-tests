namespace KubernetesWorkflow
{
    public class RunningPod
    {
        private readonly Dictionary<ContainerRecipe, Port[]> servicePortMap;

        public RunningPod(K8sCluster cluster, string name, string ip, string deploymentName, string serviceName, Dictionary<ContainerRecipe, Port[]> servicePortMap)
        {
            Cluster = cluster;
            Name = name;
            Ip = ip;
            DeploymentName = deploymentName;
            ServiceName = serviceName;
            this.servicePortMap = servicePortMap;
        }

        public K8sCluster Cluster { get; }
        public string Name { get; }
        public string Ip { get; }
        internal string DeploymentName { get; }
        internal string ServiceName { get; }

        public Port[] GetServicePortsForContainerRecipe(ContainerRecipe containerRecipe)
        {
            return servicePortMap[containerRecipe];
        }
    }
}
