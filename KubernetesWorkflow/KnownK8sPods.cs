namespace KubernetesWorkflow
{
    public class KnownK8sPods
    {
        private readonly List<string> knownActivePodNames = new List<string>();

        public bool Contains(string name)
        {
            return knownActivePodNames.Contains(name);
        }

        public void Add(string name)
        {
            knownActivePodNames.Add(name);
        }
    }
}
