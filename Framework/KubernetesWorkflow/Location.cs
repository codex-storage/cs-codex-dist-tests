namespace KubernetesWorkflow
{
    public interface ILocation
    {
    }

    public class Location : ILocation
    {
        internal Location(K8sNodeLabel? nodeLabel = null)
        {
            NodeLabel = nodeLabel;
        }

        internal K8sNodeLabel? NodeLabel { get; }

        public override string ToString()
        {
            if (NodeLabel == null) return "Location:Unspecified";
            return $"Location:KubeNode-'{NodeLabel.Key}:{NodeLabel.Value}'";
        }
    }
}
