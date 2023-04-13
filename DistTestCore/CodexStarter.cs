using DistTestCore.Codex;
using KubernetesWorkflow;

namespace DistTestCore
{
    public class CodexStarter
    {
        private readonly TestLifecycle lifecycle;
        private readonly WorkflowCreator workflowCreator;

        public CodexStarter(TestLifecycle lifecycle, WorkflowCreator workflowCreator)
        {
            this.lifecycle = lifecycle;
            this.workflowCreator = workflowCreator;
        }

        public List<CodexNodeGroup> RunningGroups { get; } = new List<CodexNodeGroup>();

        public ICodexNodeGroup BringOnline(CodexSetup codexSetup)
        {
            var containers = StartCodexContainers(codexSetup);

            var metricAccessFactory = lifecycle.PrometheusStarter.CollectMetricsFor(codexSetup, containers);
            
            var codexNodeFactory = new CodexNodeFactory(lifecycle, metricAccessFactory);

            var group = CreateCodexGroup(codexSetup, containers, codexNodeFactory);

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
        
        private RunningContainers StartCodexContainers(CodexSetup codexSetup)
        {
            Log($"Starting {codexSetup.Describe()}...");

            var workflow = CreateWorkflow();
            var startupConfig = new StartupConfig();
            startupConfig.Add(codexSetup);

            return workflow.Start(codexSetup.NumberOfNodes, codexSetup.Location, new CodexContainerRecipe(), startupConfig);
        }

        private CodexNodeGroup CreateCodexGroup(CodexSetup codexSetup, RunningContainers runningContainers, CodexNodeFactory codexNodeFactory)
        {
            var group = new CodexNodeGroup(lifecycle, codexSetup, runningContainers, codexNodeFactory);
            RunningGroups.Add(group);

            Log($"Started at '{group.Containers.RunningPod.Ip}'");
            return group;
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
