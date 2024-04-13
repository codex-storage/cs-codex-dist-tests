using KubernetesWorkflow.Recipe;
using Logging;
using Newtonsoft.Json;
using Utils;

namespace KubernetesWorkflow.Types
{
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
        public RunningPod RunningPod { get; internal set; } = null!;

        public Address GetAddress(ILog log, string portTag)
        {
            var addresses = Addresses.Where(a => a.PortTag == portTag).ToArray();
            if (!addresses.Any()) throw new Exception("No addresses found for portTag: " + portTag);

            var select = SelectAddress(addresses);
            log.Debug($"Container '{Name}' selected for tag '{portTag}' address: '{select}'");
            return select.Address;
        }

        public Address GetInternalAddress(string portTag)
        {
            var containerAddress = Addresses.Single(a => a.PortTag == portTag && a.IsInteral);
            return containerAddress.Address;
        }

        private ContainerAddress SelectAddress(ContainerAddress[] addresses)
        {
            var location = RunnerLocationUtils.GetRunnerLocation();
            if (location == RunnerLocation.InternalToCluster)
            {
                return addresses.Single(a => a.IsInteral);
            }
            if (location == RunnerLocation.ExternalToCluster)
            {
                return addresses.Single(a => !a.IsInteral);
            }
            throw new Exception("Running location not known.");
        }
    }
}
