using Newtonsoft.Json;
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
        public RunningContainer(RunningPod pod, ContainerRecipe recipe, Port[] servicePorts, string name, ContainerPort[] containerPorts)
        {
            Pod = pod;
            Recipe = recipe;
            ServicePorts = servicePorts;
            Name = name;
            ContainerPorts = containerPorts;
        }

        public string Name { get; }
        public RunningPod Pod { get; }
        public ContainerRecipe Recipe { get; }
        public Port[] ServicePorts { get; }
        public ContainerPort[] ContainerPorts { get; }

        public Address GetAddress(string portTag)
        {
            var containerPort = ContainerPorts.Single(c => c.Port.Tag == portTag);
            if (RunnerLocationUtils.DetermineRunnerLocation(this) == RunnerLocation.InternalToCluster)
            {
                return containerPort.InternalAddress;
            }
            if (containerPort.ExternalAddress == Address.InvalidAddress) throw new Exception($"Getting address by tag {portTag} resulted in an invalid address.");
            return containerPort.ExternalAddress;
        }
    }

    public class ContainerPort
    {
        public ContainerPort(Port port, Address externalAddress, Address internalAddress)
        {
            Port = port;
            ExternalAddress = externalAddress;
            InternalAddress = internalAddress;
        }

        public Port Port { get; }
        public Address ExternalAddress { get; }
        public Address InternalAddress { get; }
    }

    public static class RunningContainersExtensions
    {
        public static RunningContainer[] Containers(this RunningContainers[] runningContainers)
        {
            return runningContainers.SelectMany(c => c.Containers).ToArray();
        }

        public static string Describe(this RunningContainers[] runningContainers)
        {
            return string.Join(",", runningContainers.Select(c => c.Describe()));
        }
    }
}
