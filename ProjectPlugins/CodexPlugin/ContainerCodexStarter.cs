using CodexClient;
using CodexClient.Hooks;
using Core;
using KubernetesWorkflow;
using KubernetesWorkflow.Types;
using Logging;
using Utils;

namespace CodexPlugin
{
    public class ContainerCodexStarter : ICodexStarter
    {
        private readonly IPluginTools pluginTools;
        private readonly CodexHooksFactory hooksFactory;
        private readonly CodexContainerRecipe recipe = new CodexContainerRecipe();
        private readonly ApiChecker apiChecker;
        private readonly Dictionary<string, CodexContainerProcessControl> processControlMap = new Dictionary<string, CodexContainerProcessControl>();
        private DebugInfoVersion? versionResponse;

        public ContainerCodexStarter(IPluginTools pluginTools, CodexHooksFactory hooksFactory)
        {
            this.pluginTools = pluginTools;
            this.hooksFactory = hooksFactory;
            apiChecker = new ApiChecker(pluginTools);
        }

        public IProcessControl CreateProcessControl(ICodexInstance instance)
        {
            return processControlMap[instance.Name];
        }

        public ICodexInstance[] BringOnline(CodexSetup codexSetup)
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

            return containers.Select(CreateInstance).ToArray();
        }

        public ICodexNodeGroup WrapCodexContainers(ICodexInstance[] instances)
        {
            var codexNodeFactory = new CodexNodeFactory(
                log: pluginTools.GetLog(),
                fileManager: pluginTools.GetFileManager(),
                hooksFactory: hooksFactory,
                httpFactory: pluginTools,
                processControlFactory: this);

            var group = CreateCodexGroup(instances, codexNodeFactory);

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

        private CodexNodeGroup CreateCodexGroup(ICodexInstance[] instances, CodexNodeFactory codexNodeFactory)
        {
            var nodes = instances.Select(codexNodeFactory.CreateCodexNode).ToArray();
            var group = new CodexNodeGroup(pluginTools, nodes);

            try
            {
                Stopwatch.Measure(pluginTools.GetLog(), "EnsureOnline", group.EnsureOnline);
            }
            catch
            {
                CodexNodesNotOnline(instances);
                throw;
            }

            return group;
        }

        private ICodexInstance CreateInstance(RunningPod pod)
        {
            var instance = CodexInstanceContainerExtension.CreateFromPod(pod);
            var processControl = new CodexContainerProcessControl(pluginTools, pod, onStop: () =>
            {
                processControlMap.Remove(instance.Name);
            });
            processControlMap.Add(instance.Name, processControl);
            return instance;
        }

        private void CodexNodesNotOnline(ICodexInstance[] instances)
        {
            Log("Codex nodes failed to start");
            var log = pluginTools.GetLog();
            foreach (var i in instances)
            {
                var pc = processControlMap[i.Name];
                pc.DownloadLog(log.CreateSubfile(i.Name + "_failed_to_start"));
            }
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

    public class CodexContainerProcessControl : IProcessControl
    {
        private readonly IPluginTools tools;
        private readonly RunningPod pod;
        private readonly Action onStop;
        private readonly ContainerCrashWatcher crashWatcher;

        public CodexContainerProcessControl(IPluginTools tools, RunningPod pod, Action onStop)
        {
            this.tools = tools;
            this.pod = pod;
            this.onStop = onStop;

            crashWatcher = tools.CreateWorkflow().CreateCrashWatcher(pod.Containers.Single());
            crashWatcher.Start();
        }

        public void Stop(bool waitTillStopped)
        {
            Log($"Stopping node...");
            var workflow = tools.CreateWorkflow();
            workflow.Stop(pod, waitTillStopped);
            crashWatcher.Stop();
            onStop();
            Log("Stopped.");
        }

        public IDownloadedLog DownloadLog(LogFile file)
        {
            var workflow = tools.CreateWorkflow();
            return workflow.DownloadContainerLog(pod.Containers.Single());
        }

        public void DeleteDataDirFolder()
        {
            var container = pod.Containers.Single();

            try
            {
                var dataDirVar = container.Recipe.EnvVars.Single(e => e.Name == "CODEX_DATA_DIR");
                var dataDir = dataDirVar.Value;
                var workflow = tools.CreateWorkflow();
                workflow.ExecuteCommand(container, "rm", "-Rfv", $"/codex/{dataDir}/repo");
                Log("Deleted repo folder.");
            }
            catch (Exception e)
            {
                Log("Unable to delete repo folder: " + e);
            }
        }

        public bool HasCrashed()
        {
            return crashWatcher.HasCrashed();
        }

        private void Log(string message)
        {
            tools.GetLog().Log(message);
        }
    }
}
