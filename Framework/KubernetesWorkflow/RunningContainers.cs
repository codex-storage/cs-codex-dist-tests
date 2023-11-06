using Newtonsoft.Json;
using Utils;

namespace KubernetesWorkflow
{
    public class RunningContainers
    {
        public RunningContainers(StartupConfig startupConfig, StartResult startResult, RunningContainer[] containers)
        {
            StartupConfig = startupConfig;
            StartResult = startResult;
            Containers = containers;

            foreach (var c in containers) c.RunningContainers = this;
        }

        public StartupConfig StartupConfig { get; }
        public StartResult StartResult { get; }
        public RunningContainer[] Containers { get; }

        [JsonIgnore]
        public string Name
        {
            get { return $"{Containers.Length}x '{Containers.First().Name}'"; }
        }

        public string Describe()
        {
            return string.Join(",", Containers.Select(c => c.Name));
        }
    }

    public class RunningContainer
    {
        public RunningContainer(string name, ContainerRecipe recipe, ContainerAddress[] addresses)
        {
            Name = name;
            Recipe = recipe;
            Addresses = addresses;
        }

        public string Name { get; }
        public ContainerRecipe Recipe { get; }
        public ContainerAddress[] Addresses { get; }

        [JsonIgnore]
        public RunningContainers RunningContainers { get; internal set; } = null!;

        public Address GetAddress(string portTag)
        {
            var containerAddress = Addresses.Single(a => a.PortTag == portTag);
            if (containerAddress.IsInteral && RunningContainers.StartResult.RunnerLocation == RunnerLocation.ExternalToCluster)
            {
                throw new Exception("Attempt to access a container address created from an Internal port, " +
                    "while runner is located external to the cluster.");
            }
            return containerAddress.Address;
        }

        public Address GetInternalAddress(string portTag)
        {
            var containerAddress = Addresses.Single(a => a.PortTag == portTag);
            if (!containerAddress.IsInteral) throw new Exception(portTag + " refers to an external port");
            return containerAddress.Address;
        }
    }

    public class ContainerAddress
    {
        public ContainerAddress(string portTag, Address address, bool isInteral)
        {
            PortTag = portTag;
            Address = address;
            IsInteral = isInteral;
        }

        public string PortTag { get; }
        public Address Address { get; }
        public bool IsInteral { get; }

        public override string ToString()
        {
            return $"{PortTag} -> '{Address}'";
        }
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
