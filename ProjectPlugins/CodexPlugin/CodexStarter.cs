using Core;
using KubernetesWorkflow;
using Logging;

namespace CodexPlugin
{
    public class CodexStarter
    {
        private readonly IPluginTools pluginTools;
        private readonly CodexContainerRecipe recipe = new CodexContainerRecipe();
        private CodexDebugVersionResponse? versionResponse;

        public CodexStarter(IPluginTools pluginTools)
        {
            this.pluginTools = pluginTools;
        }

        public RunningContainers[] BringOnline(CodexSetup codexSetup)
        {
            LogSeparator();
            Log($"Starting {codexSetup.Describe()}...");

            var startupConfig = CreateStartupConfig(codexSetup);

            var containers = StartCodexContainers(startupConfig, codexSetup.NumberOfNodes, codexSetup.Location);

            var podInfos = string.Join(", ", containers.Containers().Select(c => $"Container: '{c.Name}' runs at '{c.Pod.PodInfo.K8SNodeName}'={c.Pod.PodInfo.Ip}"));
            Log($"Started {codexSetup.NumberOfNodes} nodes of image '{containers.Containers().First().Recipe.Image}'. ({podInfos})");
            LogSeparator();

            return containers;
        }

        public ICodexNodeGroup WrapCodexContainers(CoreInterface coreInterface, RunningContainers[] containers)
        {
            var codexNodeFactory = new CodexNodeFactory(pluginTools);

            var group = CreateCodexGroup(coreInterface, containers, codexNodeFactory);

            Log($"Codex version: {group.Version}");
            versionResponse = group.Version;

            return group;
        }

        public void BringOffline(CodexNodeGroup group)
        {
            Log($"Stopping {group.Describe()}...");
            StopCrashWatcher(group);
            var workflow = pluginTools.CreateWorkflow();
            foreach (var c in group.Containers)
            {
                workflow.Stop(c);
            }
            Log("Stopped.");
        }

        public string GetCodexId()
        {
            if (versionResponse != null) return versionResponse.version;
            return recipe.Image;
        }

        private StartupConfig CreateStartupConfig(CodexSetup codexSetup)
        {
            var startupConfig = new StartupConfig();
            startupConfig.NameOverride = codexSetup.NameOverride;
            startupConfig.Add(codexSetup);
            return startupConfig;
        }

        private RunningContainers[] StartCodexContainers(StartupConfig startupConfig, int numberOfNodes, ILocation location)
        {
            var result = new List<RunningContainers>();
            for (var i = 0; i < numberOfNodes; i++)
            {
                var workflow = pluginTools.CreateWorkflow();
                result.Add(workflow.Start(1, location, recipe, startupConfig));
            }
            return result.ToArray();
        }

        private CodexNodeGroup CreateCodexGroup(CoreInterface coreInterface, RunningContainers[] runningContainers, CodexNodeFactory codexNodeFactory)
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

        private void CodexNodesNotOnline(CoreInterface coreInterface, RunningContainers[] runningContainers)
        {
            Log("Codex nodes failed to start");
            foreach (var container in runningContainers.Containers()) coreInterface.DownloadLog(container);
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
