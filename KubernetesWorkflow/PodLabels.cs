using Logging;

namespace KubernetesWorkflow
{
    public class PodLabels
    {
        private readonly Dictionary<string, string> labels = new Dictionary<string, string>();

        public PodLabels(string testsType, string codexId)
        {
            Add("tests-type", testsType);
            Add("runid", NameUtils.GetRunId());
            Add("testid", NameUtils.GetTestId());
            Add("category", NameUtils.GetCategoryName());
            Add("codexid", codexId);
            Add("fixturename", NameUtils.GetRawFixtureName());
            Add("testname", NameUtils.GetTestMethodName());
        }

        public void AddAppName(string appName)
        {
            Add("app", appName);
        }

        private void Add(string key, string value)
        {
            labels.Add(key, value.ToLowerInvariant());
        }

        internal Dictionary<string, string> GetLabels()
        {
            return labels;
        }
    }
}
