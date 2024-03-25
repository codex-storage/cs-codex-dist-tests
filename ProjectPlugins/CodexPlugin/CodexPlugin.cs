using Core;
using KubernetesWorkflow.Types;

namespace CodexPlugin
{
    public class CodexPlugin : IProjectPlugin, IHasLogPrefix, IHasMetadata
    {
        private readonly CodexStarter codexStarter;
        private readonly IPluginTools tools;
        private readonly CodexLogLevel defaultLogLevel = CodexLogLevel.Trace;

        private const string OpenApiYamlHash = "8B-DD-61-54-42-D7-28-8F-5A-A0-AF-C2-A4-53-A7-08-B6-C7-02-FD-59-1A-01-A9-B4-7D-E4-81-FA-84-23-7F";

        public CodexPlugin(IPluginTools tools)
        {
            codexStarter = new CodexStarter(tools);
            this.tools = tools;
        }

        public string LogPrefix => "(Codex) ";

        public void Announce()
        {
            tools.GetLog().Log($"Loaded with Codex ID: '{codexStarter.GetCodexId()}' - Revision: {codexStarter.GetCodexRevision()}");
        }

        public void AddMetadata(IAddMetadata metadata)
        {
            metadata.Add("codexid", codexStarter.GetCodexId());
            metadata.Add("codexrevision", codexStarter.GetCodexRevision());
        }

        public void Decommission()
        {
        }

        public RunningContainers[] DeployCodexNodes(int numberOfNodes, Action<ICodexSetup> setup)
        {
            var codexSetup = GetSetup(numberOfNodes, setup);
            return codexStarter.BringOnline(codexSetup);
        }

        public ICodexNodeGroup WrapCodexContainers(CoreInterface coreInterface, RunningContainers[] containers)
        {
            containers = containers.Select(c => SerializeGate.Gate(c)).ToArray();
            return codexStarter.WrapCodexContainers(coreInterface, containers);
        }

        public void WireUpMarketplace(ICodexNodeGroup result, Action<ICodexSetup> setup)
        {
            var codexSetup = GetSetup(1, setup);
            if (codexSetup.MarketplaceConfig == null) return;
            
            var mconfig = codexSetup.MarketplaceConfig;
            foreach (var node in result)
            {
                mconfig.GethNode.SendEth(node, mconfig.InitialEth);
                mconfig.CodexContracts.MintTestTokens(node, mconfig.InitialTokens);
            }
        }

        private CodexSetup GetSetup(int numberOfNodes, Action<ICodexSetup> setup)
        {
            var codexSetup = new CodexSetup(numberOfNodes);
            codexSetup.LogLevel = defaultLogLevel;
            setup(codexSetup);
            return codexSetup;
        }
    }
}
