using DistTestCore;
using DistTestCore.Codex;
using DistTestCore.Marketplace;
using KubernetesWorkflow;

namespace CodexNetDeployer
{
    public class CodexNodeStarter
    {
        private readonly Configuration config;
        private readonly WorkflowCreator workflowCreator;
        private readonly TestLifecycle lifecycle;
        private readonly GethStartResult gethResult;
        private string bootstrapSpr = "";
        private int validatorsLeft;

        public CodexNodeStarter(Configuration config, WorkflowCreator workflowCreator, TestLifecycle lifecycle, GethStartResult gethResult, int numberOfValidators)
        {
            this.config = config;
            this.workflowCreator = workflowCreator;
            this.lifecycle = lifecycle;
            this.gethResult = gethResult;
            this.validatorsLeft = numberOfValidators;
        }

        public RunningContainer? Start(int i)
        {
            Console.Write($" - {i} = ");
            var workflow = workflowCreator.CreateWorkflow();
            var workflowStartup = new StartupConfig();
            workflowStartup.Add(gethResult);
            workflowStartup.Add(CreateCodexStartupConfig(bootstrapSpr, i, validatorsLeft));

            var containers = workflow.Start(1, Location.Unspecified, new CodexContainerRecipe(), workflowStartup);

            var container = containers.Containers.First();
            var codexAccess = new CodexAccess(lifecycle, container);

            var account = gethResult.MarketplaceNetwork.Bootstrap.AllAccounts.Accounts[i];
            var tokenAddress = gethResult.MarketplaceNetwork.Marketplace.TokenAddress;
            var marketAccess = new MarketplaceAccess(lifecycle, gethResult.MarketplaceNetwork, account, codexAccess);

            try
            {
                var debugInfo = codexAccess.Node.GetDebugInfo();
                if (!string.IsNullOrWhiteSpace(debugInfo.spr))
                {
                    Console.Write("Online\t");

                    var interaction = gethResult.MarketplaceNetwork.Bootstrap.StartInteraction(lifecycle);
                    interaction.MintTestTokens(new[] { account.Account }, config.InitialTestTokens, tokenAddress);
                    Console.Write("Tokens minted\t");

                    var response = marketAccess.MakeStorageAvailable(
                        totalSpace: config.StorageSell!.Value.MB(),
                        minPriceForTotalSpace: config.MinPrice.TestTokens(),
                        maxCollateral: config.MaxCollateral.TestTokens(),
                        maxDuration: TimeSpan.FromSeconds(config.MaxDuration));

                    if (!string.IsNullOrEmpty(response))
                    {
                        Console.Write("Storage available\tOK" + Environment.NewLine);

                        if (string.IsNullOrEmpty(bootstrapSpr)) bootstrapSpr = debugInfo.spr;
                        validatorsLeft--;
                        return container;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception:" + ex.ToString());
            }

            Console.Write("Unknown failure. Downloading container log." + Environment.NewLine);
            lifecycle.DownloadLog(container);

            return null;
        }

        private CodexStartupConfig CreateCodexStartupConfig(string bootstrapSpr, int i, int validatorsLeft)
        {
            var codexStart = new CodexStartupConfig(config.CodexLogLevel);

            if (!string.IsNullOrEmpty(bootstrapSpr)) codexStart.BootstrapSpr = bootstrapSpr;
            codexStart.StorageQuota = config.StorageQuota!.Value.MB();
            var marketplaceConfig = new MarketplaceInitialConfig(100000.Eth(), 0.TestTokens(), validatorsLeft > 0);
            marketplaceConfig.AccountIndexOverride = i;
            codexStart.MarketplaceConfig = marketplaceConfig;

            return codexStart;
        }
    }
}
