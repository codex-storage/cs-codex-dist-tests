namespace KubernetesWorkflow
{
    public class PodLabels
    {
        private readonly Dictionary<string, string> labels = new Dictionary<string, string>();

        public void Add(string key, string value)
        {
            labels.Add(key, value.ToLowerInvariant());
        }

        internal Dictionary<string, string> GetLabels()
        {
            return labels;
        }
    }
}
