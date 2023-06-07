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
        public RunningContainer(RunningPod pod, ContainerRecipe recipe, Port[] servicePorts, StartupConfig startupConfig, RunningContainerAddress clusterExternalAddress, RunningContainerAddress clusterInternalAddress)
        {
            Pod = pod;
            Recipe = recipe;
            ServicePorts = servicePorts;
            Name = GetContainerName(recipe, startupConfig);
            ClusterExternalAddress = clusterExternalAddress;
            ClusterInternalAddress = clusterInternalAddress;
        }

        public string Name { get; }
        public RunningPod Pod { get; }
        public ContainerRecipe Recipe { get; }
        public Port[] ServicePorts { get; }
        public RunningContainerAddress ClusterExternalAddress { get; }
        public RunningContainerAddress ClusterInternalAddress { get; }

        private string GetContainerName(ContainerRecipe recipe, StartupConfig startupConfig)
        {
            if (!string.IsNullOrEmpty(startupConfig.NameOverride))
            {
                return $"<{startupConfig.NameOverride}{recipe.Number}>";
            }
            else
            {
                return $"<{recipe.Name}>";
            }
        }
    }

    public class RunningContainerAddress
    {
        public RunningContainerAddress(string host, int port)
        {
            Host = host;
            Port = port;
        }

        public string Host { get; }
        public int Port { get; }
    }
}
