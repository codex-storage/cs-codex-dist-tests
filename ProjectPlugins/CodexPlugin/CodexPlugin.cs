using CodexClient;
using CodexClient.Hooks;
using Core;

namespace CodexPlugin
{
    public class CodexPlugin : IProjectPlugin, IHasLogPrefix, IHasMetadata
    {
        private const bool UseContainers = true;

        private readonly ICodexStarter codexStarter;
        private readonly IPluginTools tools;
        private readonly CodexLogLevel defaultLogLevel = CodexLogLevel.Trace;
        private readonly CodexHooksFactory hooksFactory = new CodexHooksFactory();
        private readonly ProcessControlMap processControlMap = new ProcessControlMap();
        private readonly CodexWrapper codexWrapper;

        public CodexPlugin(IPluginTools tools)
        {
            this.tools = tools;

            codexStarter = CreateCodexStarter();
            codexWrapper = new CodexWrapper(tools, processControlMap, hooksFactory);
        }

        private ICodexStarter CreateCodexStarter()
        {
            if (UseContainers)
            {
                Log("Using Containerized Codex instances");
                return new ContainerCodexStarter(tools, processControlMap);
            }

            Log("Using Binary Codex instances");
            return new BinaryCodexStarter(tools, processControlMap);
        }

        public string LogPrefix => "(Codex) ";

        public void Announce()
        {
            Log($"Loaded with Codex ID: '{codexWrapper.GetCodexId()}' - Revision: {codexWrapper.GetCodexRevision()}");
        }

        public void AddMetadata(IAddMetadata metadata)
        {
            metadata.Add("codexid", codexWrapper.GetCodexId());
            metadata.Add("codexrevision", codexWrapper.GetCodexRevision());
        }

        public void Decommission()
        {
            codexStarter.Decommission();
        }

        public ICodexInstance[] DeployCodexNodes(int numberOfNodes, Action<ICodexSetup> setup)
        {
            var codexSetup = GetSetup(numberOfNodes, setup);
            return codexStarter.BringOnline(codexSetup);
        }

        public ICodexNodeGroup WrapCodexContainers(ICodexInstance[] instances)
        {
            instances = instances.Select(c => SerializeGate.Gate(c as CodexInstance)).ToArray();
            return codexWrapper.WrapCodexInstances(instances);
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

        public void AddCodexHooksProvider(ICodexHooksProvider hooksProvider)
        {
            if (hooksFactory.Providers.Contains(hooksProvider)) return;
            hooksFactory.Providers.Add(hooksProvider);
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
