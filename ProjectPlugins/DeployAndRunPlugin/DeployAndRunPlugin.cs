using Core;
using KubernetesWorkflow;

namespace DeployAndRunPlugin
{
    public class DeployAndRunPlugin : IProjectPlugin
    {
        private readonly IPluginTools tools;

        public DeployAndRunPlugin(IPluginTools tools)
        {
            this.tools = tools;
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

            var containers = workflow.Start(1, new DeployAndRunContainerRecipe(), startupConfig);
            return containers.Containers.Single();
        }
    }
}
