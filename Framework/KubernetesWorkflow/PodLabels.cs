namespace KubernetesWorkflow
{
    public class PodLabels
    {
        private readonly Dictionary<string, string> labels = new Dictionary<string, string>();

        public void Add(string key, string value)
        {
            labels.Add(key, K8sNameUtils.Format(value));
        }

        public PodLabels Clone()
        {
            var result = new PodLabels();
            foreach (var entry in labels) result.Add(entry.Key, entry.Value);
            return result;
        }

        public void Clear()
        {
            labels.Clear();
        }

        internal Dictionary<string, string> GetLabels()
        {
            return labels;
        }
    }
}
