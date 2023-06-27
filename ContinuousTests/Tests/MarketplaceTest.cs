using DistTestCore;
using DistTestCore.Codex;
using DistTestCore.Marketplace;
using KubernetesWorkflow;
using Logging;
using Newtonsoft.Json;
using NUnit.Framework;

namespace ContinuousTests.Tests
{
    public class MarketplaceTest : ContinuousTest
    {
        public override int RequiredNumberOfNodes => 1;
        public override TimeSpan RunTestEvery => TimeSpan.FromDays(4);
        public override TestFailMode TestFailMode => TestFailMode.StopAfterFirstFailure;

        public const int EthereumAccountIndex = 200; // TODO: Check against all other account indices of all other tests.
        public const string MarketplaceTestNamespace = "codex-continuous-marketplace"; // prevent clashes too

        private readonly uint numberOfSlots = 3;
        private readonly ByteSize fileSize = 10.MB();
        private readonly TestToken pricePerSlotPerSecond = 10.TestTokens();

        private TestFile file = null!;
        private ContentId? cid;
        private string purchaseId = string.Empty;

        [TestMoment(t: Zero)]
        public void NodePostsStorageRequest()
        {
            var contractDuration = TimeSpan.FromDays(3) + TimeSpan.FromHours(1);
            decimal totalDurationSeconds = Convert.ToDecimal(contractDuration.TotalSeconds);
            var expectedTotalCost = numberOfSlots * pricePerSlotPerSecond.Amount * (totalDurationSeconds + 1);
            Log.Log("expected total cost: " + expectedTotalCost);

            file = FileManager.GenerateTestFile(fileSize);

            var (workflowCreator, lifecycle) = CreateFacilities();
            var flow = workflowCreator.CreateWorkflow();

            try
            {
                var debugInfo = Nodes[0].GetDebugInfo();
                Assert.That(!string.IsNullOrEmpty(debugInfo.spr));

                var startupConfig = new StartupConfig();
                var codexStartConfig = new CodexStartupConfig(CodexLogLevel.Debug);
                codexStartConfig.MarketplaceConfig = new MarketplaceInitialConfig(0.Eth(), 0.TestTokens(), false);
                codexStartConfig.MarketplaceConfig.AccountIndexOverride = EthereumAccountIndex;
                codexStartConfig.BootstrapSpr = debugInfo.spr;
                startupConfig.Add(codexStartConfig);
                startupConfig.Add(Configuration.CodexDeployment.GethStartResult);
                var rc = flow.Start(1, Location.Unspecified, new CodexContainerRecipe(), startupConfig);

                var account = Configuration.CodexDeployment.GethStartResult.MarketplaceNetwork.Bootstrap.AllAccounts.Accounts[EthereumAccountIndex];
                var tokenAddress = Configuration.CodexDeployment.GethStartResult.MarketplaceNetwork.Marketplace.TokenAddress;

                var interaction = Configuration.CodexDeployment.GethStartResult.MarketplaceNetwork.Bootstrap.StartInteraction(lifecycle);
                interaction.MintTestTokens(new[] { account.Account }, expectedTotalCost, tokenAddress);

                var container = rc.Containers[0];
                var marketplaceNetwork = Configuration.CodexDeployment.GethStartResult.MarketplaceNetwork;
                var codexAccess = new CodexAccess(lifecycle, container);
                var marketAccess = new MarketplaceAccess(lifecycle, marketplaceNetwork, account, codexAccess);

                cid = UploadFile(codexAccess.Node, file);
                Assert.That(cid, Is.Not.Null);

                var balance = marketAccess.GetBalance();
                Log.Log("Account: " + account.Account);
                Log.Log("Balance: " + balance);

                purchaseId = marketAccess.RequestStorage(
                    contentId: cid!,
                    pricePerSlotPerSecond: pricePerSlotPerSecond,
                    requiredCollateral: 100.TestTokens(),
                    minRequiredNumberOfNodes: numberOfSlots,
                    proofProbability: 10,
                    duration: contractDuration);

                Log.Log($"PurchaseId: '{purchaseId}'");
                Assert.That(!string.IsNullOrEmpty(purchaseId));
            }
            finally
            {
                flow.DeleteTestResources();
            }
        }

        [TestMoment(t: DayThree)]
        public void StoredDataIsAvailableAfterThreeDays()
        {
            var (workflowCreator, lifecycle) = CreateFacilities();
            var flow = workflowCreator.CreateWorkflow();
          
            try
            {
                var debugInfo = Nodes[0].GetDebugInfo();
                Assert.That(!string.IsNullOrEmpty(debugInfo.spr));

                var startupConfig = new StartupConfig();
                var codexStartConfig = new CodexStartupConfig(CodexLogLevel.Debug);
                codexStartConfig.BootstrapSpr = debugInfo.spr;
                startupConfig.Add(codexStartConfig);
                var rc = flow.Start(1, Location.Unspecified, new CodexContainerRecipe(), startupConfig);
                var container = rc.Containers[0];
                var codexAccess = new CodexAccess(lifecycle, container);

                var result = DownloadContent(codexAccess.Node, cid!);

                file.AssertIsEqual(result);
            }
            finally
            {
                flow.DeleteTestResources();
            }
        }

        private (WorkflowCreator, TestLifecycle) CreateFacilities()
        {
            var kubeConfig = GetKubeConfig(Configuration.KubeConfigFile);
            var lifecycleConfig = new DistTestCore.Configuration
            (
                kubeConfigFile: kubeConfig,
                logPath: "null",
                logDebug: false,
                dataFilesPath: Configuration.LogPath,
                codexLogLevel: CodexLogLevel.Debug,
                runnerLocation: TestRunnerLocation.ExternalToCluster
            );

            var kubeFlowConfig = new KubernetesWorkflow.Configuration(
                k8sNamespacePrefix: MarketplaceTestNamespace,
                kubeConfigFile: kubeConfig,
                operationTimeout: TimeSet.K8sOperationTimeout(),
            retryDelay: TimeSet.WaitForK8sServiceDelay());

            var workflowCreator = new WorkflowCreator(Log, kubeFlowConfig, testNamespacePostfix: string.Empty);
            var lifecycle = new TestLifecycle(new NullLog(), lifecycleConfig, TimeSet, workflowCreator);

            return (workflowCreator, lifecycle);
        }

        private string? GetKubeConfig(string kubeConfigFile)
        {
            if (string.IsNullOrEmpty(kubeConfigFile) || kubeConfigFile.ToLowerInvariant() == "null") return null;
            return kubeConfigFile;
        }
    }
}
