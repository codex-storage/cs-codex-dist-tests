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

        public List<CodexNodeGroup> RunningGroups { get; } = new List<CodexNodeGroup>();

        public ICodexNodeGroup BringOnline(CodexSetup codexSetup)
        {
            Log($"Starting {codexSetup.Describe()}...");

            var workflow = CreateWorkflow();
            var startupConfig = new StartupConfig();
            startupConfig.Add(codexSetup);
            
            var runningContainers = workflow.Start(codexSetup.NumberOfNodes, codexSetup.Location, new CodexContainerRecipe(), startupConfig);

            var group = new CodexNodeGroup(lifecycle, codexSetup, runningContainers);
            RunningGroups.Add(group);

            Log($"Started at '{group.Containers.RunningPod.Ip}'");
            return group;
        }

        public void BringOffline(CodexNodeGroup group)
        {
            Log($"Stopping {group.Describe()}...");
            var workflow = CreateWorkflow();
            workflow.Stop(group.Containers);
            RunningGroups.Remove(group);
            Log("Stopped.");
        }

        public void DeleteAllResources()
        {
            var workflow = CreateWorkflow();
            workflow.DeleteAllResources();

            RunningGroups.Clear();
        }

        public void DownloadLog(RunningContainer container, ILogHandler logHandler)
        {
            var workflow = CreateWorkflow();
            workflow.DownloadContainerLog(container, logHandler);
        }

        private StartupWorkflow CreateWorkflow()
        {
            return workflowCreator.CreateWorkflow();
        }

        private void Log(string msg)
        {
            lifecycle.Log.Log(msg);
        }
    }
}
