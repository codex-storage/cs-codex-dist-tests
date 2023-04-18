using KubernetesWorkflow;

namespace DistTestCore.Marketplace
{
    public class GethCompanionNodeStarter : BaseStarter
    {
        public GethCompanionNodeStarter(TestLifecycle lifecycle, WorkflowCreator workflowCreator)
            : base(lifecycle, workflowCreator)
        {
        }

        public GethCompanionNodeInfo[] StartCompanionNodesFor(CodexSetup codexSetup, GethBootstrapNodeInfo bootstrapNode)
        {
            LogStart($"Initializing companions for {codexSetup.NumberOfNodes} Codex nodes.");

            var startupConfig = CreateCompanionNodeStartupConfig(bootstrapNode);

            var workflow = workflowCreator.CreateWorkflow();
            var containers = workflow.Start(codexSetup.NumberOfNodes, Location.Unspecified, new GethContainerRecipe(), startupConfig);
            if (containers.Containers.Length != codexSetup.NumberOfNodes) throw new InvalidOperationException("Expected a Geth companion node to be created for each Codex node. Test infra failure.");

            LogEnd("Initialized companion nodes.");

            return containers.Containers.Select(c => CreateCompanionInfo(workflow, c)).ToArray();
        }

        private GethCompanionNodeInfo CreateCompanionInfo(StartupWorkflow workflow, RunningContainer container)
        {
            var extractor = new ContainerInfoExtractor(workflow, container);
            var account = extractor.ExtractAccount();
            return new GethCompanionNodeInfo(container, account);
        }

        private StartupConfig CreateCompanionNodeStartupConfig(GethBootstrapNodeInfo bootstrapNode)
        {
            var config = new StartupConfig();
            config.Add(new GethStartupConfig(false, bootstrapNode.GenesisJsonBase64, bootstrapNode));
            return config;
        }
    }
}
