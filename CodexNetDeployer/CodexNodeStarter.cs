using DistTestCore;
using DistTestCore.Codex;
using DistTestCore.Marketplace;
using KubernetesWorkflow;

namespace CodexNetDeployer
{
    public class CodexNodeStarter
    {
        private readonly Configuration config;
        private readonly TestLifecycle lifecycle;
        private readonly GethStartResult gethResult;
        private string bootstrapSpr = "";
        private int validatorsLeft;

        public CodexNodeStarter(Configuration config, TestLifecycle lifecycle, GethStartResult gethResult, int numberOfValidators)
        {
            this.config = config;
            this.lifecycle = lifecycle;
            this.gethResult = gethResult;
            validatorsLeft = numberOfValidators;
        }

        public CodexNodeStartResult? Start(int i)
        {
            Console.Write($" - {i} = ");
            var workflow = lifecycle.WorkflowCreator.CreateWorkflow();
            var workflowStartup = new StartupConfig();
            workflowStartup.Add(gethResult);
            workflowStartup.Add(CreateCodexStartupConfig(bootstrapSpr, i, validatorsLeft));
            workflowStartup.NameOverride = GetCodexContainerName(i);

            var containers = workflow.Start(1, Location.Unspecified, new CodexContainerRecipe(), workflowStartup);

            var container = containers.Containers.First();
            var codexAccess = new CodexAccess(lifecycle.Log, container, lifecycle.TimeSet, lifecycle.Configuration.GetAddress(container));
            var account = gethResult.MarketplaceNetwork.Bootstrap.AllAccounts.Accounts[i];
            var tokenAddress = gethResult.MarketplaceNetwork.Marketplace.TokenAddress;
            var marketAccess = new MarketplaceAccess(lifecycle, gethResult.MarketplaceNetwork, account, codexAccess);

            try
            {
                var debugInfo = codexAccess.GetDebugInfo();
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
                        return new CodexNodeStartResult(workflow, container, codexAccess);
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

        private string GetCodexContainerName(int i)
        {
            if (i == 0) return "BOOTSTRAP";
            return "CODEX" + i;
        }

        private CodexStartupConfig CreateCodexStartupConfig(string bootstrapSpr, int i, int validatorsLeft)
        {
            var codexStart = new CodexStartupConfig(config.CodexLogLevel);

            if (!string.IsNullOrEmpty(bootstrapSpr)) codexStart.BootstrapSpr = bootstrapSpr;
            codexStart.StorageQuota = config.StorageQuota!.Value.MB();
            var marketplaceConfig = new MarketplaceInitialConfig(100000.Eth(), 0.TestTokens(), validatorsLeft > 0);
            marketplaceConfig.AccountIndexOverride = i;
            codexStart.MarketplaceConfig = marketplaceConfig;
            codexStart.MetricsMode = config.Metrics;

            if (config.BlockTTL != Configuration.SecondsIn1Day)
            {
                codexStart.BlockTTL = config.BlockTTL;
            }
            if (config.BlockMI != Configuration.TenMinutes)
            {
                codexStart.BlockMaintenanceInterval = TimeSpan.FromSeconds(config.BlockMI);
            }
            if (config.BlockMN != 1000)
            {
                codexStart.BlockMaintenanceNumber = config.BlockMN;
            }

            return codexStart;
        }
    }

    public class CodexNodeStartResult
    {
        public CodexNodeStartResult(StartupWorkflow workflow, RunningContainer container, CodexAccess access)
        {
            Workflow = workflow;
            Container = container;
            Access = access;
        }

        public StartupWorkflow Workflow { get; }
        public RunningContainer Container { get; }
        public CodexAccess Access { get; }
    }
}
