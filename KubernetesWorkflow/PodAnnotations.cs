using Logging;

namespace KubernetesWorkflow
{
    public class PodAnnotations
    {
        private readonly Dictionary<string, string> annotations = new Dictionary<string, string>();

        private PodAnnotations(PodAnnotations source)
        {
            annotations = source.annotations.ToDictionary(p => p.Key, p => p.Value);
        }

        public PodAnnotations(ApplicationIds applicationIds)
        {
            if (applicationIds == null) return;
        }

        public PodAnnotations GetAnnotationsForAppName(string appName)
        {
            var pa = new PodAnnotations(this);
            if (appName == "codex") pa.Add("prometheus.io/scrape", "true");
            return pa;
        }

        private void Add(string key, string value)
        {
            annotations.Add(key, value);
        }

        internal Dictionary<string, string> GetAnnotations()
        {
            return annotations;
        }
    }
}
