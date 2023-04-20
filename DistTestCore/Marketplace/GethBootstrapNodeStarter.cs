using KubernetesWorkflow;

namespace DistTestCore.Marketplace
{
    public class GethBootstrapNodeStarter : BaseStarter
    {
        public GethBootstrapNodeStarter(TestLifecycle lifecycle, WorkflowCreator workflowCreator)
            : base(lifecycle, workflowCreator)
        {
        }

        public GethBootstrapNodeInfo StartGethBootstrapNode()
        {
            LogStart("Starting Geth bootstrap node...");
            var startupConfig = CreateBootstrapStartupConfig();
            
            var workflow = workflowCreator.CreateWorkflow();
            var containers = workflow.Start(1, Location.Unspecified, new GethContainerRecipe(), startupConfig);
            if (containers.Containers.Length != 1) throw new InvalidOperationException("Expected 1 Geth bootstrap node to be created. Test infra failure.");
            var bootstrapContainer = containers.Containers[0];

            var extractor = new ContainerInfoExtractor(workflow, bootstrapContainer);
            var account = extractor.ExtractAccount();
            var pubKey = extractor.ExtractPubKey();
            var privateKey = extractor.ExtractBootstrapPrivateKey();
            var discoveryPort = bootstrapContainer.Recipe.GetPortByTag(GethContainerRecipe.DiscoveryPortTag);

            LogEnd($"Geth bootstrap node started with account '{account}'");

            return new GethBootstrapNodeInfo(containers, account, pubKey, privateKey, discoveryPort);
        }

        private StartupConfig CreateBootstrapStartupConfig()
        {
            var config = new StartupConfig();
            config.Add(new GethStartupConfig(true, null!));
            return config;
        }
    }
}
