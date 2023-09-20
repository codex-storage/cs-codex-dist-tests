using Core;

namespace GethPlugin
{
    public class GethPlugin : IProjectPlugin, IHasLogPrefix, IHasMetadata
    {
        private readonly GethStarter starter;
        private readonly IPluginTools tools;

        public GethPlugin(IPluginTools tools)
        {
            starter = new GethStarter(tools);
            this.tools = tools;
        }

        public string LogPrefix => "(Geth) ";

        public void Announce()
        {
            tools.GetLog().Log($"Loaded Geth plugin.");
        }

        public void AddMetadata(IAddMetadata metadata)
        {
            metadata.Add("gethid", GethContainerRecipe.DockerImage);
        }

        public void Decommission()
        {
        }

        public IGethDeployment DeployGeth(Action<IGethSetup> setup)
        {
            var startupConfig = new GethStartupConfig();
            setup(startupConfig);
            return starter.StartGeth(startupConfig);
        }

        public IGethNode WrapGethDeployment(IGethDeployment startResult)
        {
            return starter.WrapGethContainer(SerializeGate.Gate(startResult));
        }
    }
}
