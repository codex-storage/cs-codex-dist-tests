using Core;
using GethPlugin;

namespace CodexContractsPlugin
{
    public class CodexContractsPlugin : IProjectPlugin, IHasLogPrefix, IHasMetadata
    {
        private readonly IPluginTools tools;
        private readonly CodexContractsStarter starter;

        public CodexContractsPlugin(IPluginTools tools)
        {
            this.tools = tools;
            starter = new CodexContractsStarter(tools);
        }

        public string LogPrefix => "(CodexContracts) ";

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

        public ICodexContracts DeployContracts(IGethNode gethNode)
        {
            return starter.Start(gethNode);
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
