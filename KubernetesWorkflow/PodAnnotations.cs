namespace KubernetesWorkflow
{
    public class PodAnnotations
    {
        private readonly Dictionary<string, string> annotations = new Dictionary<string, string>();

        public void Add(string key, string value)
        {
            annotations.Add(key, value);
        }

        public PodAnnotations Clone()
        {
            var result = new PodAnnotations();
            foreach (var entry in annotations) result.Add(entry.Key, entry.Value);
            return result;
        }

        public void Clear()
        {
            annotations.Clear();
        }

        internal Dictionary<string, string> GetAnnotations()
        {
            return annotations;
        }
    }
}
