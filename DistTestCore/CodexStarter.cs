using DistTestCore.Codex;
using KubernetesWorkflow;
using Logging;

namespace DistTestCore
{
    public class CodexStarter
    {
        private readonly WorkflowCreator workflowCreator;

        public CodexStarter(TestLog log, Configuration configuration)
        {
            workflowCreator = new WorkflowCreator(configuration.GetK8sConfiguration());
        }

        public ICodexNodeGroup BringOnline(CodexSetupConfig codexSetupConfig)
        {
            var workflow = workflowCreator.CreateWorkflow();
            var startupConfig = new StartupConfig();
            startupConfig.Add(codexSetupConfig);
            
            var runningContainers = workflow.Start(codexSetupConfig.NumberOfNodes, codexSetupConfig.Location, new CodexContainerRecipe(), startupConfig);

            // create access objects. Easy, right?
        }

        public void DeleteAllResources()
        {
            var workflow = workflowCreator.CreateWorkflow();
            workflow.DeleteAllResources();
        }
    }
}
