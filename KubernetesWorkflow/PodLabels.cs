namespace KubernetesWorkflow
{
    public class PodLabels
    {
        private readonly Dictionary<string, string> labels = new Dictionary<string, string>();

        public void Add(string key, string value)
        {
            labels.Add(key, Format(value));
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

        private static string Format(string s)
        {
            var result = s.ToLowerInvariant()
                .Replace(":", "-")
                .Replace("/", "-")
                .Replace("\\", "-")
                .Replace("[", "-")
                .Replace("]", "-")
                .Replace(",", "-");

            return result.Trim('-');
        }

        internal Dictionary<string, string> GetLabels()
        {
            return labels;
        }
    }
}
