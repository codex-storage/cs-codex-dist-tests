using DistTestCore.Codex;
using DistTestCore.Marketplace;
using KubernetesWorkflow;

namespace DistTestCore
{
    public class CodexStarter : BaseStarter
    {
        public CodexStarter(TestLifecycle lifecycle, WorkflowCreator workflowCreator)
            : base(lifecycle, workflowCreator)
        {
        }

        public List<CodexNodeGroup> RunningGroups { get; } = new List<CodexNodeGroup>();

        public ICodexNodeGroup BringOnline(CodexSetup codexSetup)
        {
            LogSeparator();
            LogStart($"Starting {codexSetup.Describe()}...");
            var gethStartResult = lifecycle.GethStarter.BringOnlineMarketplaceFor(codexSetup);

            var startupConfig = CreateStartupConfig(gethStartResult, codexSetup);
            var containers = StartCodexContainers(startupConfig, codexSetup.NumberOfNodes, codexSetup.Location);

            var metricAccessFactory = lifecycle.PrometheusStarter.CollectMetricsFor(codexSetup, containers);
            
            var codexNodeFactory = new CodexNodeFactory(lifecycle, metricAccessFactory, gethStartResult.MarketplaceAccessFactory);

            var group = CreateCodexGroup(codexSetup, containers, codexNodeFactory);
            LogEnd($"Started {codexSetup.NumberOfNodes} nodes at '{group.Containers.RunningPod.Ip}'. They are: {group.Describe()}");
            LogSeparator();
            return group;
        }

        public void BringOffline(CodexNodeGroup group)
        {
            LogStart($"Stopping {group.Describe()}...");
            var workflow = CreateWorkflow();
            workflow.Stop(group.Containers);
            RunningGroups.Remove(group);
            LogEnd("Stopped.");
        }

        public void DeleteAllResources()
        {
            var workflow = CreateWorkflow();
            workflow.DeleteTestResources();

            RunningGroups.Clear();
        }

        public void DownloadLog(RunningContainer container, ILogHandler logHandler)
        {
            var workflow = CreateWorkflow();
            workflow.DownloadContainerLog(container, logHandler);
        }

        private StartupConfig CreateStartupConfig(GethStartResult gethStartResult, CodexSetup codexSetup)
        {
            var startupConfig = new StartupConfig();
            startupConfig.NameOverride = codexSetup.NameOverride;
            startupConfig.Add(codexSetup);
            startupConfig.Add(gethStartResult);
            return startupConfig;
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
            group.EnsureOnline();
            return group;
        }

        private StartupWorkflow CreateWorkflow()
        {
            return workflowCreator.CreateWorkflow();
        }

        private void LogSeparator()
        {
            Log("----------------------------------------------------------------------------");
        }
    }
}
