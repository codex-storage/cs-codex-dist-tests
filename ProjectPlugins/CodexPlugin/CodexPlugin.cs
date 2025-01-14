using CodexPlugin.Hooks;
using Core;
using KubernetesWorkflow.Types;

namespace CodexPlugin
{
    public class CodexPlugin : IProjectPlugin, IHasLogPrefix, IHasMetadata
    {
        private readonly CodexStarter codexStarter;
        private readonly IPluginTools tools;
        private readonly CodexLogLevel defaultLogLevel = CodexLogLevel.Trace;

        public CodexPlugin(IPluginTools tools)
        {
            codexStarter = new CodexStarter(tools);
            this.tools = tools;
        }

        public string LogPrefix => "(Codex) ";

        public void Announce()
        {
            Log($"Loaded with Codex ID: '{codexStarter.GetCodexId()}' - Revision: {codexStarter.GetCodexRevision()}");
        }

        public void AddMetadata(IAddMetadata metadata)
        {
            metadata.Add("codexid", codexStarter.GetCodexId());
            metadata.Add("codexrevision", codexStarter.GetCodexRevision());
        }

        public void Decommission()
        {
        }

        public RunningPod[] DeployCodexNodes(int numberOfNodes, Action<ICodexSetup> setup)
        {
            var codexSetup = GetSetup(numberOfNodes, setup);
            return codexStarter.BringOnline(codexSetup);
        }

        public ICodexNodeGroup WrapCodexContainers(CoreInterface coreInterface, RunningPod[] containers)
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
                mconfig.GethNode.SendEth(node, mconfig.MarketplaceSetup.InitialEth);
                mconfig.CodexContracts.MintTestTokens(node, mconfig.MarketplaceSetup.InitialTestTokens);

                Log($"Send {mconfig.MarketplaceSetup.InitialEth} and " +
                    $"minted {mconfig.MarketplaceSetup.InitialTestTokens} for " +
                    $"{node.GetName()} (address: {node.EthAddress})");
            }
        }

        public void SetCodexHooksProvider(ICodexHooksProvider hooksProvider)
        {
            codexStarter.HooksFactory.Provider = hooksProvider;
        }

        private CodexSetup GetSetup(int numberOfNodes, Action<ICodexSetup> setup)
        {
            var codexSetup = new CodexSetup(numberOfNodes);
            codexSetup.LogLevel = defaultLogLevel;
            setup(codexSetup);
            return codexSetup;
        }

        private void Log(string msg)
        {
            tools.GetLog().Log(msg);
        }
    }
}
