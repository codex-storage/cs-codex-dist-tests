namespace KubernetesWorkflow
{
    public class PodAnnotations
    {
        private readonly Dictionary<string, string> annotations = new Dictionary<string, string>();

        public void Add(string key, string value)
        {
            annotations.Add(key, value);
        }

        internal Dictionary<string, string> GetAnnotations()
        {
            return annotations;
        }
    }
}
