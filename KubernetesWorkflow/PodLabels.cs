using Logging;

namespace KubernetesWorkflow
{
    public class PodLabels
    {
        private readonly Dictionary<string, string> labels = new Dictionary<string, string>();

        private PodLabels(PodLabels source)
        {
            labels = source.labels.ToDictionary(p => p.Key, p => p.Value);
        }

        public PodLabels(string testsType, ApplicationIds applicationIds)
        {
            Add("tests-type", testsType);
            Add("runid", NameUtils.GetRunId());
            Add("testid", NameUtils.GetTestId());
            Add("category", NameUtils.GetCategoryName());
            Add("fixturename", NameUtils.GetRawFixtureName());
            Add("testname", NameUtils.GetTestMethodName());

            if (applicationIds == null) return;
            Add("codexid", applicationIds.CodexId);
            Add("gethid", applicationIds.GethId);
            Add("prometheusid", applicationIds.PrometheusId);
            Add("codexcontractsid", applicationIds.CodexContractsId);
            Add("grafanaid", applicationIds.GrafanaId);
        }

        public PodLabels GetLabelsForAppName(string appName)
        {
            var pl = new PodLabels(this);
            pl.Add("app", appName);
            return pl;
        }

        private void Add(string key, string value)
        {
            labels.Add(key, Format(value));
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
