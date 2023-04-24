using KubernetesWorkflow;

namespace DistTestCore.Marketplace
{
    public class GethCompanionNodeStarter : BaseStarter
    {
        public GethCompanionNodeStarter(TestLifecycle lifecycle, WorkflowCreator workflowCreator)
            : base(lifecycle, workflowCreator)
        {
        }

        public GethCompanionNodeInfo[] StartCompanionNodesFor(CodexSetup codexSetup, MarketplaceNetwork marketplace)
        {
            LogStart($"Initializing companions for {codexSetup.NumberOfNodes} Codex nodes.");

            var startupConfig = CreateCompanionNodeStartupConfig(marketplace.Bootstrap);

            var workflow = workflowCreator.CreateWorkflow();
            var containers = workflow.Start(codexSetup.NumberOfNodes, Location.Unspecified, new GethContainerRecipe(), startupConfig);
            if (containers.Containers.Length != codexSetup.NumberOfNodes) throw new InvalidOperationException("Expected a Geth companion node to be created for each Codex node. Test infra failure.");

            var result = containers.Containers.Select(c => CreateCompanionInfo(workflow, c)).ToArray();

            foreach (var node in result)
            {
                EnsureCompanionNodeIsSynced(node, marketplace);
            }

            LogEnd($"Initialized {codexSetup.NumberOfNodes} companion nodes. Their accounts: [{string.Join(",", result.Select(c => c.Account))}]");

            return result;
        }

        private GethCompanionNodeInfo CreateCompanionInfo(StartupWorkflow workflow, RunningContainer container)
        {
            var extractor = new ContainerInfoExtractor(workflow, container);
            var account = extractor.ExtractAccount();
            var privKey = extractor.ExtractPrivateKey();
            return new GethCompanionNodeInfo(container, account, privKey);
        }

        private void EnsureCompanionNodeIsSynced(GethCompanionNodeInfo node, MarketplaceNetwork marketplace)
        {
            try
            {
                var interaction = node.StartInteraction(lifecycle.Log);
                interaction.EnsureSynced(marketplace.Marketplace.Address, marketplace.Marketplace.Abi);
            }
            catch (Exception e)
            {
                throw new Exception("Geth companion node did not sync within timeout. Test infra failure.", e);
            }
        }

        private StartupConfig CreateCompanionNodeStartupConfig(GethBootstrapNodeInfo bootstrapNode)
        {
            var config = new StartupConfig();
            config.Add(new GethStartupConfig(false, bootstrapNode));
            return config;
        }
    }
}
