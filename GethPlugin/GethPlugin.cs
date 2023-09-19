using Core;

namespace GethPlugin
{
    public class GethPlugin : IProjectPlugin, IHasLogPrefix, IHasMetadata
    {
        private readonly GethStarter starter;

        public GethPlugin(IPluginTools tools)
        {
            starter = new GethStarter(tools);
        }

        public string LogPrefix => "(Geth) ";

        public void Announce()
        {
            //tools.GetLog().Log($"Loaded with Codex ID: '{codexStarter.GetCodexId()}'");
        }

        public void AddMetadata(IAddMetadata metadata)
        {
            //metadata.Add("codexid", codexStarter.GetCodexId());
        }

        public void Decommission()
        {
        }

        public IGethStartResult StartGeth(Action<IGethSetup> setup)
        {
            var startupConfig = new GethStartupConfig();
            setup(startupConfig);
            return starter.StartGeth(startupConfig);
        }

        public IGethNode WrapGethContainer(IGethStartResult startResult)
        {
            return starter.WrapGethContainer(startResult);
        }
    }
}
