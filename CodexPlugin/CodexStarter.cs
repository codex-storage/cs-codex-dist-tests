using Core;
using KubernetesWorkflow;
using Logging;

namespace CodexPlugin
{
    public class CodexStarter
    {
        private readonly IPluginTools pluginTools;

        public CodexStarter(IPluginTools pluginTools)
        {
            this.pluginTools = pluginTools;
        }

        public RunningContainers[] BringOnline(CodexSetup codexSetup)
        {
            LogSeparator();
            Log($"Starting {codexSetup.Describe()}...");
            //var gethStartResult = lifecycle.GethStarter.BringOnlineMarketplaceFor(codexSetup);

            var startupConfig = CreateStartupConfig(/*gethStartResult,*/ codexSetup);

            var containers = StartCodexContainers(startupConfig, codexSetup.NumberOfNodes, codexSetup.Location);

            //var metricAccessFactory = CollectMetrics(codexSetup, containers);

            //var codexNodeFactory = new CodexNodeFactory(lifecycle, metricAccessFactory, gethStartResult.MarketplaceAccessFactory);

            //var group = CreateCodexGroup(codexSetup, containers, codexNodeFactory);
            //lifecycle.SetCodexVersion(group.Version);

            var podInfos = string.Join(", ", containers.Containers().Select(c => $"Container: '{c.Name}' runs at '{c.Pod.PodInfo.K8SNodeName}'={c.Pod.PodInfo.Ip}"));
            Log($"Started {codexSetup.NumberOfNodes} nodes of image '{containers.Containers().First().Recipe.Image}'. ({podInfos})");
            LogSeparator();

            return containers;
        }

        public ICodexNodeGroup WrapCodexContainers(RunningContainers[] containers)
        {
            //var metricAccessFactory = CollectMetrics(codexSetup, containers);

            var codexNodeFactory = new CodexNodeFactory(pluginTools);// (lifecycle, metricAccessFactory, gethStartResult.MarketplaceAccessFactory);

            var group = CreateCodexGroup(containers, codexNodeFactory);

            Log($"Codex version: {group.Version}");

            //lifecycle.SetCodexVersion(group.Version);

            return group;
        }

        public void BringOffline(CodexNodeGroup group)
        {
            //LogStart($"Stopping {group.Describe()}...");
            //var workflow = CreateWorkflow();
            //foreach (var c in group.Containers)
            //{
            //    StopCrashWatcher(c);
            //    workflow.Stop(c);
            //}
            //LogEnd("Stopped.");
        }

        public void DeleteAllResources()
        {
            //var workflow = CreateWorkflow();
            //workflow.DeleteTestResources();
        }

        public void DownloadLog(RunningContainer container, ILogHandler logHandler, int? tailLines)
        {
            //var workflow = CreateWorkflow();
            //workflow.DownloadContainerLog(container, logHandler, tailLines);
        }

        //private IMetricsAccessFactory CollectMetrics(CodexSetup codexSetup, RunningContainers[] containers)
        //{
        //    if (codexSetup.MetricsMode == MetricsMode.None) return new MetricsUnavailableAccessFactory();

        //    var runningContainers = lifecycle.PrometheusStarter.CollectMetricsFor(containers);

        //    if (codexSetup.MetricsMode == MetricsMode.Dashboard)
        //    {
        //        lifecycle.GrafanaStarter.StartDashboard(runningContainers.Containers.First(), codexSetup);
        //    }

        //    return new CodexNodeMetricsAccessFactory(lifecycle, runningContainers);
        //}

        private StartupConfig CreateStartupConfig(/*GethStartResult gethStartResult, */ CodexSetup codexSetup)
        {
            var startupConfig = new StartupConfig();
            startupConfig.NameOverride = codexSetup.NameOverride;
            startupConfig.CreateCrashWatcher = true;
            startupConfig.Add(codexSetup);
            //startupConfig.Add(gethStartResult);
            return startupConfig;
        }

        private RunningContainers[] StartCodexContainers(StartupConfig startupConfig, int numberOfNodes, Location location)
        {
            var result = new List<RunningContainers>();
            var recipe = new CodexContainerRecipe();
            for (var i = 0; i < numberOfNodes; i++)
            {
                var workflow = pluginTools.CreateWorkflow();
                result.Add(workflow.Start(1, location, recipe, startupConfig));
            }
            return result.ToArray();
        }

        private CodexNodeGroup CreateCodexGroup(RunningContainers[] runningContainers, CodexNodeFactory codexNodeFactory)
        {
            var group = new CodexNodeGroup(pluginTools, runningContainers, codexNodeFactory);

            try
            {
                Stopwatch.Measure(pluginTools.GetLog(), "EnsureOnline", group.EnsureOnline);
            }
            catch
            {
                CodexNodesNotOnline(runningContainers);
                throw;
            }

            return group;
        }

        private void CodexNodesNotOnline(RunningContainers[] runningContainers)
        {
            Log("Codex nodes failed to start");
            // todo:
            //foreach (var container in runningContainers.Containers()) lifecycle.DownloadLog(container);
        }

        //private StartupWorkflow CreateWorkflow()
        //{
        //    return lifecycle.WorkflowCreator.CreateWorkflow();
        //}

        private void LogSeparator()
        {
            Log("----------------------------------------------------------------------------");
        }

        private void Log(string message)
        {
            pluginTools.GetLog().Log(message);
        }

        //private void StopCrashWatcher(RunningContainers containers)
        //{
        //    foreach (var c in containers.Containers)
        //    {
        //        c.CrashWatcher?.Stop();
        //    }
        //}
    }
}
