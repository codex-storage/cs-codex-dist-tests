using KubernetesWorkflow;
using Utils;

namespace DistTestCore.Marketplace
{
    public class GethCompanionNodeStarter : BaseStarter
    {
        public GethCompanionNodeStarter(TestLifecycle lifecycle, WorkflowCreator workflowCreator)
            : base(lifecycle, workflowCreator)
        {
        }

        public GethCompanionNodeInfo StartCompanionNodeFor(CodexSetup codexSetup, MarketplaceNetwork marketplace)
        {
            LogStart($"Initializing companion for {codexSetup.NumberOfNodes} Codex nodes.");

            var startupConfig = CreateCompanionNodeStartupConfig(marketplace.Bootstrap, codexSetup.NumberOfNodes);

            var workflow = workflowCreator.CreateWorkflow();
            var containers = workflow.Start(1, Location.Unspecified, new GethContainerRecipe(), startupConfig);
            WaitForAccountCreation(codexSetup.NumberOfNodes);
            if (containers.Containers.Length != 1) throw new InvalidOperationException("Expected one Geth companion node to be created. Test infra failure.");
            var container = containers.Containers[0];

            var node = CreateCompanionInfo(workflow, container, codexSetup.NumberOfNodes);
            EnsureCompanionNodeIsSynced(node, marketplace);

            LogEnd($"Initialized one companion node for {codexSetup.NumberOfNodes} Codex nodes. Their accounts: [{string.Join(",", node.Accounts.Select(a => a.Account))}]");
            return node;
        }

        private void WaitForAccountCreation(int numberOfNodes)
        {
            // We wait proportional to the number of account the node has to create. It takes a few seconds for each one to generate the keys and create the files
            // we will be trying to read in 'ExtractAccount', later on in the start-up process.
            Time.Sleep(TimeSpan.FromSeconds(4.5 * numberOfNodes));
        }

        private GethCompanionNodeInfo CreateCompanionInfo(StartupWorkflow workflow, RunningContainer container, int numberOfAccounts)
        {
            var extractor = new ContainerInfoExtractor(lifecycle.Log, workflow, container);
            var accounts = ExtractAccounts(extractor, numberOfAccounts).ToArray();
            return new GethCompanionNodeInfo(container, accounts);
        }

        private IEnumerable<GethCompanionAccount> ExtractAccounts(ContainerInfoExtractor extractor, int numberOfAccounts)
        {
            for (int i = 0; i < numberOfAccounts; i++) yield return ExtractAccount(extractor, i + 1);
        }

        private GethCompanionAccount ExtractAccount(ContainerInfoExtractor extractor, int orderNumber)
        {
            var account = extractor.ExtractAccount(orderNumber);
            var privKey = extractor.ExtractPrivateKey(orderNumber);
            return new GethCompanionAccount(account, privKey);
        }

        private void EnsureCompanionNodeIsSynced(GethCompanionNodeInfo node, MarketplaceNetwork marketplace)
        {
            try
            {
                var interaction = node.StartInteraction(lifecycle.Log, node.Accounts.First());
                interaction.EnsureSynced(marketplace.Marketplace.Address, marketplace.Marketplace.Abi);
            }
            catch (Exception e)
            {
                throw new Exception("Geth companion node did not sync within timeout. Test infra failure.", e);
            }
        }

        private StartupConfig CreateCompanionNodeStartupConfig(GethBootstrapNodeInfo bootstrapNode, int numberOfAccounts)
        {
            var config = new StartupConfig();
            config.Add(new GethStartupConfig(false, bootstrapNode, numberOfAccounts));
            return config;
        }
    }
}
