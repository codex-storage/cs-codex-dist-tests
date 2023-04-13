using DistTestCore.Codex;
using KubernetesWorkflow;

namespace DistTestCore
{
    public class CodexStarter
    {
        private readonly WorkflowCreator workflowCreator;
        private readonly TestLifecycle lifecycle;

        public CodexStarter(TestLifecycle lifecycle, Configuration configuration)
        {
            workflowCreator = new WorkflowCreator(configuration.GetK8sConfiguration());
            this.lifecycle = lifecycle;
        }

        public ICodexNodeGroup BringOnline(CodexSetup codexSetup)
        {
            var workflow = CreateWorkflow();
            var startupConfig = new StartupConfig();
            startupConfig.Add(codexSetup);
            
            var runningContainers = workflow.Start(codexSetup.NumberOfNodes, codexSetup.Location, new CodexContainerRecipe(), startupConfig);

            return new CodexNodeGroup(lifecycle, codexSetup, runningContainers);
        }

        public void BringOffline(RunningContainers runningContainers)
        {
            var workflow = CreateWorkflow();
            workflow.Stop(runningContainers);
        }

        public void DeleteAllResources()
        {
            var workflow = CreateWorkflow();
            workflow.DeleteAllResources();
        }

        private StartupWorkflow CreateWorkflow()
        {
            return workflowCreator.CreateWorkflow();
        }
    }
}
