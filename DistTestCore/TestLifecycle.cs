using DistTestCore.Codex;
using KubernetesWorkflow;

namespace DistTestCore
{
    public class TestLifecycle
    {
        private readonly WorkflowCreator workflowCreator = new WorkflowCreator();

        public void SetUpCodexNodes()
        {
            var config = new CodexStartupConfig()
            {
                StorageQuota = 10.MB(),
                Location = Location.Unspecified,
                LogLevel = CodexLogLevel.Error,
                MetricsEnabled = false,
            };

            var workflow = workflowCreator.CreateWorkflow();
            var startupConfig = new StartupConfig();
            startupConfig.Add(config);
            var containers = workflow.Start(3, new CodexContainerRecipe(), startupConfig);

            foreach (var c in containers.Containers)
            {
                var access = new CodexAccess(c);
            }
        }
    }
}
