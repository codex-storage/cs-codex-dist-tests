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
            Log($"Starting {codexSetup.Describe()}...");
            var gethStartResult = lifecycle.GethStarter.BringOnlineMarketplaceFor(codexSetup);

            var startupConfig = new StartupConfig();
            startupConfig.Add(codexSetup);
            startupConfig.Add(gethStartResult);

            var containers = StartCodexContainers(startupConfig, codexSetup.NumberOfNodes, codexSetup.Location);

            var metricAccessFactory = lifecycle.PrometheusStarter.CollectMetricsFor(codexSetup, containers);
            
            var codexNodeFactory = new CodexNodeFactory(lifecycle, metricAccessFactory, gethStartResult.MarketplaceAccessFactory);

            var group = CreateCodexGroup(codexSetup, containers, codexNodeFactory);
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
        
        private RunningContainers StartCodexContainers(StartupConfig startupConfig, int numberOfNodes, Location location)
        {
            var workflow = CreateWorkflow();
            return workflow.Start(numberOfNodes, location, new CodexContainerRecipe(), startupConfig);
        }

        private CodexNodeGroup CreateCodexGroup(CodexSetup codexSetup, RunningContainers runningContainers, CodexNodeFactory codexNodeFactory)
        {
            var group = new CodexNodeGroup(lifecycle, codexSetup, runningContainers, codexNodeFactory);
            RunningGroups.Add(group);
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
