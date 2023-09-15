using Core;
using KubernetesWorkflow;

namespace GethPlugin
{
    public class GethPlugin : IProjectPlugin, IHasLogPrefix, IHasMetadata
    {
        private readonly IPluginTools tools;

        public GethPlugin(IPluginTools tools)
        {
            //codexStarter = new CodexStarter(tools);
            this.tools = tools;
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
