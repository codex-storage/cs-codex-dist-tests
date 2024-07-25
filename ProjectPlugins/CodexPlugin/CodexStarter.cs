using Core;
using KubernetesWorkflow;
using KubernetesWorkflow.Types;
using Logging;

namespace CodexPlugin
{
    public class CodexStarter
    {
        private readonly IPluginTools pluginTools;
        private readonly CodexContainerRecipe recipe = new CodexContainerRecipe();
        private readonly ApiChecker apiChecker;
        private DebugInfoVersion? versionResponse;

        public CodexStarter(IPluginTools pluginTools)
        {
            this.pluginTools = pluginTools;

            apiChecker = new ApiChecker(pluginTools);
        }

        public RunningPod[] BringOnline(CodexSetup codexSetup)
        {
            LogSeparator();
            Log($"Starting {codexSetup.Describe()}...");

            var startupConfig = CreateStartupConfig(codexSetup);

            var containers = StartCodexContainers(startupConfig, codexSetup.NumberOfNodes, codexSetup.Location);

            apiChecker.CheckCompatibility(containers);

            foreach (var rc in containers)
            {
                var podInfo = GetPodInfo(rc);
                var podInfos = string.Join(", ", rc.Containers.Select(c => $"Container: '{c.Name}' PodLabel: '{c.RunningPod.StartResult.Deployment.PodLabel}' runs at '{podInfo.K8SNodeName}'={podInfo.Ip}"));
                Log($"Started node with image '{containers.First().Containers.First().Recipe.Image}'. ({podInfos})");
            }
            LogSeparator();

            return containers;
        }

        public ICodexNodeGroup WrapCodexContainers(CoreInterface coreInterface, RunningPod[] containers)
        {
            var codexNodeFactory = new CodexNodeFactory(pluginTools);

            var group = CreateCodexGroup(coreInterface, containers, codexNodeFactory);

            Log($"Codex version: {group.Version}");
            versionResponse = group.Version;

            return group;
        }

        public void BringOffline(CodexNodeGroup group, bool waitTillStopped)
        {
            Log($"Stopping {group.Describe()}...");
            StopCrashWatcher(group);
            var workflow = pluginTools.CreateWorkflow();
            foreach (var c in group.Containers)
            {
                workflow.Stop(c, waitTillStopped);
            }
            Log("Stopped.");
        }

        public void Stop(RunningPod pod, bool waitTillStopped)
        {
            Log($"Stopping node...");
            var workflow = pluginTools.CreateWorkflow();
            workflow.Stop(pod, waitTillStopped);
            Log("Stopped.");
        }

        public string GetCodexId()
        {
            if (versionResponse != null) return versionResponse.Version;
            return recipe.Image;
        }

        public string GetCodexRevision()
        {
            if (versionResponse != null) return versionResponse.Revision;
            return "unknown";
        }

        private StartupConfig CreateStartupConfig(CodexSetup codexSetup)
        {
            var startupConfig = new StartupConfig();
            startupConfig.NameOverride = codexSetup.NameOverride;
            startupConfig.Add(codexSetup);
            return startupConfig;
        }

        private RunningPod[] StartCodexContainers(StartupConfig startupConfig, int numberOfNodes, ILocation location)
        {
            var futureContainers = new List<FutureContainers>();
            for (var i = 0; i < numberOfNodes; i++)
            {
                var workflow = pluginTools.CreateWorkflow();
                futureContainers.Add(workflow.Start(1, location, recipe, startupConfig));
            }

            return futureContainers
                .Select(f => f.WaitForOnline())
                .ToArray();
        }

        private PodInfo GetPodInfo(RunningPod rc)
        {
            var workflow = pluginTools.CreateWorkflow();
            return workflow.GetPodInfo(rc);
        }

        private CodexNodeGroup CreateCodexGroup(CoreInterface coreInterface, RunningPod[] runningContainers, CodexNodeFactory codexNodeFactory)
        {
            var group = new CodexNodeGroup(this, pluginTools, runningContainers, codexNodeFactory);

            try
            {
                Stopwatch.Measure(pluginTools.GetLog(), "EnsureOnline", group.EnsureOnline);
            }
            catch
            {
                CodexNodesNotOnline(coreInterface, runningContainers);
                throw;
            }

            return group;
        }

        private void CodexNodesNotOnline(CoreInterface coreInterface, RunningPod[] runningContainers)
        {
            Log("Codex nodes failed to start");
            foreach (var container in runningContainers.First().Containers) coreInterface.DownloadLog(container);
        }

        private void LogSeparator()
        {
            Log("----------------------------------------------------------------------------");
        }

        private void Log(string message)
        {
            pluginTools.GetLog().Log(message);
        }

        private void StopCrashWatcher(CodexNodeGroup group)
        {
            foreach (var node in group)
            {
                node.CrashWatcher.Stop();
            }
        }
    }
}
