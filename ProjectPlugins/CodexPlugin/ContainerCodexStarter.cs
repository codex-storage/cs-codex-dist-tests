using CodexClient;
using Core;
using KubernetesWorkflow;
using KubernetesWorkflow.Types;
using Utils;

namespace CodexPlugin
{
    public class ContainerCodexStarter : ICodexStarter
    {
        private readonly IPluginTools pluginTools;
        private readonly ProcessControlMap processControlMap;
        private readonly CodexContainerRecipe recipe = new CodexContainerRecipe();
        private readonly ApiChecker apiChecker;

        public ContainerCodexStarter(IPluginTools pluginTools, ProcessControlMap processControlMap)
        {
            this.pluginTools = pluginTools;
            this.processControlMap = processControlMap;
            apiChecker = new ApiChecker(pluginTools);
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

        public void Decommission()
        {
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

        private ICodexInstance CreateInstance(RunningPod pod)
        {
            var instance = CodexInstanceContainerExtension.CreateFromPod(pod);
            var processControl = new CodexContainerProcessControl(pluginTools, pod, onStop: () =>
            {
                processControlMap.Remove(instance);
            });
            processControlMap.Add(instance, processControl);
            return instance;
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
