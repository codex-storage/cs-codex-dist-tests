using KubernetesWorkflow;
using Logging;

namespace DistTestCore
{
    public abstract class DefaultContainerRecipe : ContainerRecipeFactory
    {
        public static string TestsType { get; set; } = "NotSet";
        public static ApplicationIds? ApplicationIds { get; set; } = null;

        protected abstract void InitializeRecipe(StartupConfig config);

        protected override void Initialize(StartupConfig config)
        {
            Add("tests-type", TestsType);
            Add("runid", NameUtils.GetRunId());
            Add("testid", NameUtils.GetTestId());
            Add("category", NameUtils.GetCategoryName());
            Add("fixturename", NameUtils.GetRawFixtureName());
            Add("testname", NameUtils.GetTestMethodName());

            if (ApplicationIds != null)
            {
                Add("codexid", ApplicationIds.CodexId);
                Add("gethid", ApplicationIds.GethId);
                Add("prometheusid", ApplicationIds.PrometheusId);
                Add("codexcontractsid", ApplicationIds.CodexContractsId);
                Add("grafanaid", ApplicationIds.GrafanaId);
            }
            Add("app", AppName);

            InitializeRecipe(config);
        }

        private void Add(string name, string value)
        {
            AddPodLabel(name, value);
        }
    }
}
