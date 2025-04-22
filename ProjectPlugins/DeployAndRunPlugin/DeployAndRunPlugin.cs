using Core;
using KubernetesWorkflow;
using KubernetesWorkflow.Types;

namespace DeployAndRunPlugin
{
    public class DeployAndRunPlugin : IProjectPlugin
    {
        private readonly IPluginTools tools;

        public DeployAndRunPlugin(IPluginTools tools)
        {
            this.tools = tools;
        }

        public void Awake(IPluginAccess access)
        {
        }

        public void Announce()
        {
            tools.GetLog().Log("Deploy-and-Run plugin loaded.");
        }

        public void Decommission()
        {
        }

        public RunningContainer Run(RunConfig config)
        {
            var workflow = tools.CreateWorkflow();
            var startupConfig = new StartupConfig();
            startupConfig.NameOverride = "dnr-" + config.Name;
            startupConfig.Add(config);

            var location = workflow.GetAvailableLocations().Get("fixed-s-4vcpu-16gb-amd-yz8rd");
            var containers = workflow.Start(1, location, new DeployAndRunContainerRecipe(), startupConfig).WaitForOnline();
            return containers.Containers.Single();
        }
    }
}
