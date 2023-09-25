using Core;
using KubernetesWorkflow;

namespace WakuPlugin
{
    public class WakuPlugin : IProjectPlugin, IHasLogPrefix, IHasMetadata
    {
        private readonly IPluginTools tools;
        private readonly WakuStarter starter;

        public WakuPlugin(IPluginTools tools)
        {
            this.tools = tools;
            starter = new WakuStarter(tools);
        }

        public string LogPrefix => "(Waku) ";

        public void Announce()
        {
            tools.GetLog().Log($"Loaded with Waku plugin.");
        }

        public void AddMetadata(IAddMetadata metadata)
        {
            //metadata.Add("codexid", codexStarter.GetCodexId());
        }

        public void Decommission()
        {
        }

        public RunningContainers[] DeployWakuNodes(int numberOfNodes, Action<IWakuSetup> setup)
        {
            return starter.Start(numberOfNodes, setup);
        }

        public IWakuNode WrapWakuContainer(RunningContainer container)
        {
            container = SerializeGate.Gate(container);
            return starter.Wrap(container);
        }

        //public ICodexNodeGroup WrapCodexContainers(CoreInterface coreInterface, RunningContainers[] containers)
        //{
        //    containers = containers.Select(c => SerializeGate.Gate(c)).ToArray();
        //    return codexStarter.WrapCodexContainers(coreInterface, containers);
        //}

        //public void WireUpMarketplace(ICodexNodeGroup result, Action<ICodexSetup> setup)
        //{
        //    var codexSetup = GetSetup(1, setup);
        //    if (codexSetup.MarketplaceConfig == null) return;

        //    var mconfig = codexSetup.MarketplaceConfig;
        //    foreach (var node in result)
        //    {
        //        mconfig.GethNode.SendEth(node, mconfig.InitialEth);
        //        mconfig.CodexContracts.MintTestTokens(mconfig.GethNode, node, mconfig.InitialTokens);
        //    }
        //}

        //private CodexSetup GetSetup(int numberOfNodes, Action<ICodexSetup> setup)
        //{
        //    var codexSetup = new CodexSetup(numberOfNodes);
        //    codexSetup.LogLevel = defaultLogLevel;
        //    setup(codexSetup);
        //    return codexSetup;
        //}
    }
}
