using KubernetesWorkflow.Types;
using Logging;

namespace KubernetesWorkflow
{
    public class LocationProvider
    {
        private readonly TimeSpan locationsExpirationTime = TimeSpan.FromMinutes(10);
        private readonly ILog log;
        private readonly Action<Action<K8sController>> onController;
        private Location[] knownLocations = Array.Empty<Location>();
        private DateTime lastUpdate = DateTime.UtcNow;

        public LocationProvider(ILog log, Action<Action<K8sController>> onController)
        {
            this.log = log;
            this.onController = onController;
        }

        public IKnownLocations GetAvailableLocations()
        {
            if (ShouldUpdateKnownLocations())
            {
                onController(UpdateKnownLocations);
            }

            return new KnownLocations(knownLocations);
        }

        private void UpdateKnownLocations(K8sController controller)
        {
            knownLocations = controller.GetAvailableK8sNodes().Select(CreateLocation).ToArray();
            lastUpdate = DateTime.UtcNow;

            log.Log($"Detected {knownLocations.Length} available locations: '{string.Join(",", knownLocations.Select(l => l.ToString()))}'");
        }

        private Location CreateLocation(K8sNodeLabel k8sNode)
        {
            return new Location(k8sNode);
        }

        private bool ShouldUpdateKnownLocations()
        {
            if (!knownLocations.Any()) return true;
            if (DateTime.UtcNow - lastUpdate > locationsExpirationTime) return true;
            return false;
        }
    }
}
