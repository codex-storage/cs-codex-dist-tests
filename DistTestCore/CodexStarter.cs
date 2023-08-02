using DistTestCore.Codex;
using DistTestCore.Marketplace;
using DistTestCore.Metrics;
using KubernetesWorkflow;
using Logging;

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

            var metricAccessFactory = CollectMetrics(codexSetup, containers);
            
            var codexNodeFactory = new CodexNodeFactory(lifecycle, metricAccessFactory, gethStartResult.MarketplaceAccessFactory);

            var group = CreateCodexGroup(codexSetup, containers, codexNodeFactory);
            lifecycle.SetCodexVersion(group.Version);

            var podInfo = group.Containers.RunningPod.PodInfo;
            LogEnd($"Started {codexSetup.NumberOfNodes} nodes " +
                $"of image '{containers.Containers.First().Recipe.Image}' " +
                $"and version '{group.Version}' " +
                $"at location '{podInfo.K8SNodeName}'={podInfo.Ip}. " +
                $"They are: {group.Describe()}");
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

        private IMetricsAccessFactory CollectMetrics(CodexSetup codexSetup, RunningContainers containers)
        {
            if (!codexSetup.MetricsEnabled) return new MetricsUnavailableAccessFactory();

            var runningContainers = lifecycle.PrometheusStarter.CollectMetricsFor(containers);
            return new CodexNodeMetricsAccessFactory(lifecycle, runningContainers);
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
            Stopwatch.Measure(lifecycle.Log, "EnsureOnline", group.EnsureOnline, debug: true);
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
