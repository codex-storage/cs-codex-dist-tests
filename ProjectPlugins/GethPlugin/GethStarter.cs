using BlockchainUtils;
using Core;
using KubernetesWorkflow;
using Logging;

namespace GethPlugin
{
    public class GethStarter
    {
        private readonly IPluginTools tools;
        private readonly ILog log;

        public GethStarter(IPluginTools tools)
        {
            this.tools = tools;
            log = new LogPrefixer(tools.GetLog(), $"({nameof(GethStarter)}) ");
        }

        public GethDeployment StartGeth(GethStartupConfig gethStartupConfig)
        {
            Log("Starting Geth node...");

            var startupConfig = new StartupConfig();
            startupConfig.Add(gethStartupConfig);
            startupConfig.NameOverride = gethStartupConfig.NameOverride;

            var workflow = tools.CreateWorkflow();
            var containers = workflow.Start(1, new GethContainerRecipe(), startupConfig).WaitForOnline();
            if (containers.Containers.Length != 1) throw new InvalidOperationException("Expected 1 Geth bootstrap node to be created. Test infra failure.");
            var container = containers.Containers[0];

            var extractor = new GethContainerInfoExtractor(log, workflow, container);
            var account = extractor.ExtractAccounts().Accounts.First();
            var pubKey = extractor.ExtractPubKey();

            var discoveryPort = container.Recipe.GetPortByTag(GethContainerRecipe.DiscoveryPortTag);
            if (discoveryPort == null) throw new Exception("Expected discovery port to be created.");
            var httpPort = container.Recipe.GetPortByTag(GethContainerRecipe.HttpPortTag);
            if (httpPort == null) throw new Exception("Expected http port to be created.");
            var wsPort = container.Recipe.GetPortByTag(GethContainerRecipe.WsPortTag);
            if (wsPort == null) throw new Exception("Expected ws port to be created.");

            Log($"Geth node started.");

            return new GethDeployment(containers, discoveryPort, httpPort, wsPort, account, pubKey);
        }

        public IGethNode WrapGethContainer(GethDeployment startResult, BlockCache blockCache)
        {
            startResult = SerializeGate.Gate(startResult);
            var node = new DeploymentGethNode(tools.GetLog(), blockCache, startResult);
            Log($"EthAddress: {node.CurrentAddress}");
            return node;
        }

        private void Log(string msg)
        {
            log.Log(msg);
        }
    }
}
