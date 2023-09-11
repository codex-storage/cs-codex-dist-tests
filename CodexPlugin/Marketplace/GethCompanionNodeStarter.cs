//using KubernetesWorkflow;
//using Utils;

//namespace DistTestCore.Marketplace
//{
//    public class GethCompanionNodeStarter : BaseStarter
//    {
//        private int companionAccountIndex = 0;

//        public GethCompanionNodeStarter(TestLifecycle lifecycle)
//            : base(lifecycle)
//        {
//        }

//        public GethCompanionNodeInfo StartCompanionNodeFor(CodexSetup codexSetup, MarketplaceNetwork marketplace)
//        {
//            LogStart($"Initializing companion for {codexSetup.NumberOfNodes} Codex nodes.");

//            var config = CreateCompanionNodeStartupConfig(marketplace.Bootstrap, codexSetup.NumberOfNodes);

//            var workflow = lifecycle.WorkflowCreator.CreateWorkflow();
//            var containers = workflow.Start(1, Location.Unspecified, new GethContainerRecipe(), CreateStartupConfig(config));
//            if (containers.Containers.Length != 1) throw new InvalidOperationException("Expected one Geth companion node to be created. Test infra failure.");
//            var container = containers.Containers[0];

//            var node = CreateCompanionInfo(container, marketplace, config);
//            EnsureCompanionNodeIsSynced(node, marketplace);

//            LogEnd($"Initialized one companion node for {codexSetup.NumberOfNodes} Codex nodes. Their accounts: [{string.Join(",", node.Accounts.Select(a => a.Account))}]");
//            return node;
//        }

//        private GethCompanionNodeInfo CreateCompanionInfo(RunningContainer container, MarketplaceNetwork marketplace, GethStartupConfig config)
//        {
//            var accounts = ExtractAccounts(marketplace, config);
//            return new GethCompanionNodeInfo(container, accounts);
//        }

//        private static GethAccount[] ExtractAccounts(MarketplaceNetwork marketplace, GethStartupConfig config)
//        {
//            return marketplace.Bootstrap.AllAccounts.Accounts
//                .Skip(1 + config.CompanionAccountStartIndex)
//                .Take(config.NumberOfCompanionAccounts)
//                .ToArray();
//        }

//        private void EnsureCompanionNodeIsSynced(GethCompanionNodeInfo node, MarketplaceNetwork marketplace)
//        {
//            try
//            {
//                Time.WaitUntil(() =>
//                {
//                    var interaction = node.StartInteraction(lifecycle, node.Accounts.First());
//                    return interaction.IsSynced(marketplace.Marketplace.Address, marketplace.Marketplace.Abi);
//                }, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(3));
//            }
//            catch (Exception e)
//            {
//                throw new Exception("Geth companion node did not sync within timeout. Test infra failure.", e);
//            }
//        }

//        private GethStartupConfig CreateCompanionNodeStartupConfig(GethBootstrapNodeInfo bootstrapNode, int numberOfAccounts)
//        {
//            var config = new GethStartupConfig(false, bootstrapNode, companionAccountIndex, numberOfAccounts);
//            companionAccountIndex += numberOfAccounts;
//            return config;
//        }

//        private StartupConfig CreateStartupConfig(GethStartupConfig gethConfig)
//        {
//            var config = new StartupConfig();
//            config.Add(gethConfig);
//            return config;
//        }
//    }
//}
