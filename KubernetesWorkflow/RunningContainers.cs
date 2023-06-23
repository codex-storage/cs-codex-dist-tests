using Utils;

namespace KubernetesWorkflow
{
    public class RunningContainers
    {
        public RunningContainers(StartupConfig startupConfig, RunningPod runningPod, RunningContainer[] containers)
        {
            StartupConfig = startupConfig;
            RunningPod = runningPod;
            Containers = containers;
        }

        public StartupConfig StartupConfig { get; }
        public RunningPod RunningPod { get; }
        public RunningContainer[] Containers { get; }

        public string Describe()
        {
            return string.Join(",", Containers.Select(c => c.Name));
        }
    }

    public class RunningContainer
    {
        public RunningContainer(RunningPod pod, ContainerRecipe recipe, Port[] servicePorts, string name, Address clusterExternalAddress, Address clusterInternalAddress)
        {
            Pod = pod;
            Recipe = recipe;
            ServicePorts = servicePorts;
            Name = name;
            ClusterExternalAddress = clusterExternalAddress;
            ClusterInternalAddress = clusterInternalAddress;
        }

        public string Name { get; }
        public RunningPod Pod { get; }
        public ContainerRecipe Recipe { get; }
        public Port[] ServicePorts { get; }
        public Address ClusterExternalAddress { get; }
        public Address ClusterInternalAddress { get; }
    }
}
