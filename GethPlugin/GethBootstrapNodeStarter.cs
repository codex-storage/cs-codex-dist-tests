using KubernetesWorkflow;

namespace GethPlugin
{
    public class GethBootstrapNodeStarter
    {
        public GethBootstrapNodeInfo StartGethBootstrapNode()
        {
            LogStart("Starting Geth bootstrap node...");
            var startupConfig = CreateBootstrapStartupConfig();

            var workflow = lifecycle.WorkflowCreator.CreateWorkflow();
            var containers = workflow.Start(1, Location.Unspecified, new GethContainerRecipe(), startupConfig);
            if (containers.Containers.Length != 1) throw new InvalidOperationException("Expected 1 Geth bootstrap node to be created. Test infra failure.");
            var bootstrapContainer = containers.Containers[0];

            var extractor = new ContainerInfoExtractor(lifecycle.Log, workflow, bootstrapContainer);
            var accounts = extractor.ExtractAccounts();
            var pubKey = extractor.ExtractPubKey();
            var discoveryPort = bootstrapContainer.Recipe.GetPortByTag(GethContainerRecipe.DiscoveryPortTag);
            var result = new GethBootstrapNodeInfo(containers, accounts, pubKey, discoveryPort);

            LogEnd($"Geth bootstrap node started with account '{result.Account.Account}'");

            return result;
        }

        private StartupConfig CreateBootstrapStartupConfig()
        {
            var config = new StartupConfig();
            config.Add(new GethStartupConfig(true, null!, 0, 0));
            return config;
        }
    }
}
