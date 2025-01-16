using CodexPlugin.Hooks;
using Core;
using GethPlugin;
using KubernetesWorkflow;
using KubernetesWorkflow.Types;
using Logging;
using Utils;

namespace CodexPlugin
{
    public class CodexStarter : IProcessControl
    {
        private readonly IPluginTools pluginTools;
        private readonly CodexContainerRecipe recipe = new CodexContainerRecipe();
        private readonly ApiChecker apiChecker;
        private readonly Dictionary<ICodexInstance, RunningPod> podMap = new Dictionary<ICodexInstance, RunningPod>();
        private DebugInfoVersion? versionResponse;

        public CodexStarter(IPluginTools pluginTools)
        {
            this.pluginTools = pluginTools;

            apiChecker = new ApiChecker(pluginTools);
        }

        public CodexHooksFactory HooksFactory { get; } = new CodexHooksFactory();

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
                LogEthAddress(rc);
            }
            LogSeparator();

            return containers;
        }

        public void Stop(ICodexInstance instance, bool waitTillStopped)
        {
            Log($"Stopping node...");
            var pod = podMap[instance];
            podMap.Remove(instance);

            var workflow = pluginTools.CreateWorkflow();
            workflow.Stop(pod, waitTillStopped);
            Log("Stopped.");
        }

        public IDownloadedLog DownloadLog(ICodexInstance instance, LogFile file)
        {
            var workflow = pluginTools.CreateWorkflow();
            var pod = podMap[instance];
            return workflow.DownloadContainerLog(pod.Containers.Single());
        }

        public void DeleteDataDirFolder(ICodexInstance instance)
        {
            var pod = podMap[instance];
            var container = pod.Containers.Single();

            try
            {
                var dataDirVar = container.Recipe.EnvVars.Single(e => e.Name == "CODEX_DATA_DIR");
                var dataDir = dataDirVar.Value;
                var workflow = pluginTools.CreateWorkflow();
                workflow.ExecuteCommand(container, "rm", "-Rfv", $"/codex/{dataDir}/repo");
                Log("Deleted repo folder.");
            }
            catch (Exception e)
            {
                Log("Unable to delete repo folder: " + e);
            }
        }

        public ICodexNodeGroup WrapCodexContainers(CoreInterface coreInterface, RunningPod[] containers)
        {
            var codexNodeFactory = new CodexNodeFactory(pluginTools, HooksFactory);

            var group = CreateCodexGroup(coreInterface, containers, codexNodeFactory);

            Log($"Codex version: {group.Version}");
            versionResponse = group.Version;

            return group;
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
            var instances = runningContainers.Select(CreateInstance).ToArray();
            var accesses = instances.Select(CreateAccess).ToArray();
            var nodes = accesses.Select(codexNodeFactory.CreateOnlineCodexNode).ToArray();
            var group = new CodexNodeGroup(pluginTools, nodes);

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

        private CodexAccess CreateAccess(ICodexInstance instance)
        {
            var crashWatcher = CreateCrashWatcher(instance);
            return new CodexAccess(pluginTools, this, instance, crashWatcher);
        }

        private ICrashWatcher CreateCrashWatcher(ICodexInstance instance)
        {
            var pod = podMap[instance];
            return pluginTools.CreateWorkflow().CreateCrashWatcher(pod.Containers.Single());
        }

        private ICodexInstance CreateInstance(RunningPod pod)
        {
            var instance = CodexInstanceContainerExtension.CreateFromPod(pod);
            podMap.Add(instance, pod);
            return instance;
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

        private void LogEthAddress(RunningPod rc)
        {
            var account = rc.Containers.First().Recipe.Additionals.Get<EthAccount>();
            if (account == null) return;
            Log($"{rc.Name} = {account}");
        }

        private void Log(string message)
        {
            pluginTools.GetLog().Log(message);
        }
    }
}
