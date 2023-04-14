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

            var extractor = new GethInfoExtractor(workflow, containers.Containers[0]);
            var account = extractor.ExtractAccount();
            var genesisJsonBase64 = extractor.ExtractGenesisJsonBase64();

            Log($"Geth bootstrap node started with account '{account}'");

            return new GethBootstrapNodeInfo(containers, account, genesisJsonBase64);
        }

        private StartupConfig CreateBootstrapStartupConfig()
        {
            var config = new StartupConfig();
            config.Add(new GethStartupConfig(true, bootstrapGenesisJsonBase64));
            return config;
        }

        private void Log(string msg)
        {
            lifecycle.Log.Log(msg);
        }
    }
}
