using Core;
using KubernetesWorkflow;

namespace GethPlugin
{
    public class GethPlugin : IProjectPlugin, IHasLogPrefix, IHasMetadata
    {
        private readonly IPluginTools tools;
        private readonly GethStarter starter;

        public GethPlugin(IPluginTools tools)
        {
            this.tools = tools;
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

        public IGethNode StartGeth(Action<IGethSetup> setup)
        {
            var startupConfig = new GethStartupConfig();
            setup(startupConfig);
            return starter.StartGeth(startupConfig);
        }

        //public RunningContainers[] StartCodexNodes(int numberOfNodes, Action<ICodexSetup> setup)
        //{
        //    var codexSetup = new CodexSetup(numberOfNodes);
        //    codexSetup.LogLevel = defaultLogLevel;
        //    setup(codexSetup);
        //    return codexStarter.BringOnline(codexSetup);
        //}

        //public ICodexNodeGroup WrapCodexContainers(RunningContainers[] containers)
        //{
        //    return codexStarter.WrapCodexContainers(containers);
        //}
    }
}
