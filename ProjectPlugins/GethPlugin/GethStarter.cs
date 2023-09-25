using Core;
using KubernetesWorkflow;

namespace GethPlugin
{
    public class GethStarter
    {
        private readonly IPluginTools tools;

        public GethStarter(IPluginTools tools)
        {
            this.tools = tools;
        }

        public GethDeployment StartGeth(GethStartupConfig gethStartupConfig)
        {
            Log("Starting Geth node...");

            var startupConfig = new StartupConfig();
            startupConfig.Add(gethStartupConfig);
            startupConfig.NameOverride = gethStartupConfig.NameOverride;

            var workflow = tools.CreateWorkflow();
            var containers = workflow.Start(1, new GethContainerRecipe(), startupConfig);
            if (containers.Containers.Length != 1) throw new InvalidOperationException("Expected 1 Geth bootstrap node to be created. Test infra failure.");
            var container = containers.Containers[0];

            var extractor = new GethContainerInfoExtractor(tools.GetLog(), workflow, container);
            var accounts = extractor.ExtractAccounts();
            var pubKey = extractor.ExtractPubKey();

            var discoveryPort = container.Recipe.GetPortByTag(GethContainerRecipe.DiscoveryPortTag);
            if (discoveryPort == null) throw new Exception("Expected discovery port to be created.");
            var httpPort = container.Recipe.GetPortByTag(GethContainerRecipe.HttpPortTag);
            if (httpPort == null) throw new Exception("Expected http port to be created.");
            var wsPort = container.Recipe.GetPortByTag(GethContainerRecipe.WsPortTag);
            if (wsPort == null) throw new Exception("Expected ws port to be created.");

            Log($"Geth node started.");

            return new GethDeployment(container, discoveryPort, httpPort, wsPort, accounts, pubKey);
        }

        public IGethNode WrapGethContainer(GethDeployment startResult)
        {
            startResult = SerializeGate.Gate(startResult);
            return new GethNode(tools.GetLog(), startResult);
        }

        private void Log(string msg)
        {
            tools.GetLog().Log(msg);
        }
    }
}
