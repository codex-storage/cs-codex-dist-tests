using Logging;
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

        public Address GetAddress(ILog log, string portTag)
        {
            var addresses = Addresses.Where(a => a.PortTag == portTag).ToArray();
            if (!addresses.Any()) throw new Exception("No addresses found for portTag: " + portTag);

            var location = RunningContainers.StartResult.RunnerLocation;
            ContainerAddress select = null!;
            if (location == RunnerLocation.InternalToCluster)
            {
                select = addresses.Single(a => a.IsInteral);
            }
            else if (location == RunnerLocation.ExternalToCluster)
            {
                select = addresses.Single(a => !a.IsInteral);
            }
            else
            {
                throw new Exception("Running location not known.");
            }

            log.Log($"Container '{Name}' selected for tag '{portTag}' address: '{select}'");
            return select.Address;
        }

        public Address GetInternalAddress(string portTag)
        {
            var containerAddress = Addresses.Single(a => a.PortTag == portTag && a.IsInteral);
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
            var indicator = IsInteral ? "int" : "ext";
            return $"{indicator} {PortTag} -> '{Address}'";
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
