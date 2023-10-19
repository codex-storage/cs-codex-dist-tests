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

        [JsonIgnore]
        public Address Address
        {
            get
            {
                throw new Exception("a");
                //if (RunnerLocationUtils.DetermineRunnerLocation(this) == RunnerLocation.InternalToCluster)
                //{
                //    return ClusterInternalAddress;
                //}
                //return ClusterExternalAddress;
            }
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
