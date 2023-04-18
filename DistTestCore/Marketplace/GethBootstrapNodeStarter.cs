using KubernetesWorkflow;

namespace DistTestCore.Marketplace
{
    public class GethBootstrapNodeStarter
    {
        private const string bootstrapGenesisJsonBase64 = "ewogICAgImNvbmZpZyI6IHsKICAgICAgImNoYWluSWQiOiA3ODk5ODgsCiAgICAgICJob21lc3RlYWRCbG9jayI6IDAsCiAgICAgICJlaXAxNTBCbG9jayI6IDAsCiAgICAgICJlaXAxNTVCbG9jayI6IDAsCiAgICAgICJlaXAxNThCbG9jayI6IDAsCiAgICAgICJieXphbnRpdW1CbG9jayI6IDAsCiAgICAgICJjb25zdGFudGlub3BsZUJsb2NrIjogMCwKICAgICAgInBldGVyc2J1cmdCbG9jayI6IDAsCiAgICAgICJpc3RhbmJ1bEJsb2NrIjogMCwKICAgICAgIm11aXJHbGFjaWVyQmxvY2siOiAwLAogICAgICAiYmVybGluQmxvY2siOiAwLAogICAgICAibG9uZG9uQmxvY2siOiAwLAogICAgICAiYXJyb3dHbGFjaWVyQmxvY2siOiAwLAogICAgICAiZ3JheUdsYWNpZXJCbG9jayI6IDAsCiAgICAgICJjbGlxdWUiOiB7CiAgICAgICAgInBlcmlvZCI6IDUsCiAgICAgICAgImVwb2NoIjogMzAwMDAKICAgICAgfQogICAgfSwKICAgICJkaWZmaWN1bHR5IjogIjEiLAogICAgImdhc0xpbWl0IjogIjgwMDAwMDAwMCIsCiAgICAiZXh0cmFkYXRhIjogIjB4MDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMEFDQ09VTlRfSEVSRTAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAiLAogICAgImFsbG9jIjogewogICAgICAiMHhBQ0NPVU5UX0hFUkUiOiB7ICJiYWxhbmNlIjogIjUwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAiIH0KICAgIH0KICB9";
        private readonly TestLifecycle lifecycle;
        private readonly WorkflowCreator workflowCreator;

        public GethBootstrapNodeStarter(TestLifecycle lifecycle, WorkflowCreator workflowCreator)
        {
            this.lifecycle = lifecycle;
            this.workflowCreator = workflowCreator;
        }

        public GethBootstrapNodeInfo StartGethBootstrapNode()
        {
            Log("Starting Geth bootstrap node...");
            var startupConfig = CreateBootstrapStartupConfig();
            
            var workflow = workflowCreator.CreateWorkflow();
            var containers = workflow.Start(1, Location.Unspecified, new GethContainerRecipe(), startupConfig);
            if (containers.Containers.Length != 1) throw new InvalidOperationException("Expected 1 Geth bootstrap node to be created. Test infra failure.");
            var bootstrapContainer = containers.Containers[0];

            var extractor = new ContainerInfoExtractor(workflow, bootstrapContainer);
            var account = extractor.ExtractAccount();
            var genesisJsonBase64 = extractor.ExtractGenesisJsonBase64();
            var pubKey = extractor.ExtractPubKey();
            var discoveryPort = bootstrapContainer.Recipe.GetPortByTag(GethContainerRecipe.DiscoveryPortTag);

            Log($"Geth bootstrap node started with account '{account}'");

            return new GethBootstrapNodeInfo(containers, account, genesisJsonBase64, pubKey, discoveryPort);
        }

        private StartupConfig CreateBootstrapStartupConfig()
        {
            var config = new StartupConfig();
            config.Add(new GethStartupConfig(true, bootstrapGenesisJsonBase64, null!));
            return config;
        }

        private void Log(string msg)
        {
            lifecycle.Log.Log(msg);
        }
    }
}
