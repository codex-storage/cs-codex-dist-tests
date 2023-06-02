namespace KubernetesWorkflow
{
    public class RunningPod
    {
        private readonly Dictionary<ContainerRecipe, Port[]> servicePortMap;

        public RunningPod(K8sCluster cluster, PodInfo podInfo, string deploymentName, string serviceName, Dictionary<ContainerRecipe, Port[]> servicePortMap)
        {
            Cluster = cluster;
            PodInfo = podInfo;
            DeploymentName = deploymentName;
            ServiceName = serviceName;
            this.servicePortMap = servicePortMap;
        }

        public K8sCluster Cluster { get; }
        public PodInfo PodInfo { get; }
        internal string DeploymentName { get; }
        internal string ServiceName { get; }

        public Port[] GetServicePortsForContainerRecipe(ContainerRecipe containerRecipe)
        {
            return servicePortMap[containerRecipe];
        }
    }

    public class PodInfo
    {
        public PodInfo(string podName, string podIp, string k8sNodeName)
        {
            Name = podName;
            Ip = podIp;
            K8SNodeName = k8sNodeName;
        }

        public string Name { get; }
        public string Ip { get; }
        public string K8SNodeName { get; }
    }
}
