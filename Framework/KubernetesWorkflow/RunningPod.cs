namespace KubernetesWorkflow
{
    public class RunningPod
    {
        public RunningPod(K8sCluster cluster, PodInfo podInfo, string deploymentName, string serviceName,  ContainerRecipePortMapEntry[] portMapEntries)
        {
            Cluster = cluster;
            PodInfo = podInfo;
            DeploymentName = deploymentName;
            ServiceName = serviceName;
            PortMapEntries = portMapEntries;
        }

        public K8sCluster Cluster { get; }
        public PodInfo PodInfo { get; }
        public ContainerRecipePortMapEntry[] PortMapEntries { get; }
        public string DeploymentName { get; }
        public string ServiceName { get; }

        public Port[] GetServicePortsForContainerRecipe(ContainerRecipe containerRecipe)
        {
            return PortMapEntries
                .Where(p => p.ContainerNumber == containerRecipe.Number)
                .SelectMany(p => p.Ports)
                .ToArray();
        }
    }

    public class ContainerRecipePortMapEntry
    {
        public ContainerRecipePortMapEntry(int containerNumber, Port[] ports)
        {
            ContainerNumber = containerNumber;
            Ports = ports;
        }

        public int ContainerNumber { get; }
        public Port[] Ports { get; }
    }

    public class PodInfo
    {
        public PodInfo(string name, string ip, string k8sNodeName)
        {
            Name = name;
            Ip = ip;
            K8SNodeName = k8sNodeName;
        }

        public string Name { get; }
        public string Ip { get; }
        public string K8SNodeName { get; }
    }
}
