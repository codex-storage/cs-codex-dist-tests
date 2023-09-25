namespace KubernetesWorkflow
{
    public interface IKnownLocations
    {
        /// <summary>
        /// Returns a known location given an index.
        /// Each index guarantees a different location.
        /// </summary>
        ILocation Get(int index);
        int NumberOfLocations { get; }

        /// <summary>
        /// Returns the location object for a specific kubernetes node. Throws if it doesn't exist.
        /// </summary>
        ILocation Get(string kubeNodeName, bool allowPartialMatches = false);
    }

    public class KnownLocations : IKnownLocations
    {
        private readonly Location[] locations;

        public KnownLocations(Location[] locations)
        {
            this.locations = locations;
            if (locations.Any(l => l.NodeLabel == null)) throw new Exception("Must not contain unspecified location");
        }

        public static ILocation UnspecifiedLocation { get; } = new Location();

        public int NumberOfLocations => locations.Length;

        public ILocation Get(int index)
        {
            return locations[index];
        }

        public ILocation Get(string kubeNodeName, bool allowPartialMatches = false)
        {
            if (allowPartialMatches)
            {
                return locations.Single(l => l.NodeLabel != null && l.NodeLabel.Value.Contains(kubeNodeName));
            }

            return locations.Single(l => l.NodeLabel != null && l.NodeLabel.Value == kubeNodeName);
        }
    }
}
