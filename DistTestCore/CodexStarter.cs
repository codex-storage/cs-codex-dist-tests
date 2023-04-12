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
            
        }

        public void DeleteAllResources()
        {
            var workflow = workflowCreator.CreateWorkflow();
            workflow.DeleteAllResources();
        }
    }
}
