using DistTestCore;
using DistTestCore.Codex;
using DistTestCore.Marketplace;
using KubernetesWorkflow;
using Logging;
using NUnit.Framework;

namespace ContinuousTests.Tests
{
    public class MarketplaceTest : ContinuousTest
    {
        public override int RequiredNumberOfNodes => 1;
        public override TimeSpan RunTestEvery => TimeSpan.FromDays(4);
        public override TestFailMode TestFailMode => TestFailMode.AlwaysRunAllMoments;

        public const int EthereumAccountIndex = 200; // TODO: Check against all other account indices of all other tests.

        private const string MarketplaceTestNamespace = "codex-continuous-marketplace";

        private readonly ByteSize fileSize = 100.MB();
        private readonly TestToken pricePerBytePerSecond = 1.TestTokens();

        private TestFile file = null!;
        private ContentId? cid;
        private TestToken startingBalance = null!;
        private string purchaseId = string.Empty;

        [TestMoment(t: Zero)]
        public void NodePostsStorageRequest()
        {
            var contractDuration = TimeSpan.FromDays(3) + TimeSpan.FromHours(1);
            decimal totalDurationSeconds = Convert.ToDecimal(contractDuration.TotalSeconds);
            var expectedTotalCost = pricePerBytePerSecond.Amount * totalDurationSeconds;

            file = FileManager.GenerateTestFile(fileSize);

            var (workflowCreator, lifecycle) = CreateFacilities();
            var flow = workflowCreator.CreateWorkflow();
            var startupConfig = new StartupConfig();
            var codexStartConfig = new CodexStartupConfig(CodexLogLevel.Debug);
            codexStartConfig.MarketplaceConfig = new MarketplaceInitialConfig(0.Eth(), 0.TestTokens(), false);
            codexStartConfig.MarketplaceConfig.AccountIndexOverride = EthereumAccountIndex;
            startupConfig.Add(codexStartConfig);
            startupConfig.Add(Configuration.CodexDeployment.GethStartResult);
            var rc = flow.Start(1, Location.Unspecified, new CodexContainerRecipe(), startupConfig);

            try
            {
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

                startingBalance = marketAccess.GetBalance();

                purchaseId = marketAccess.RequestStorage(
                    contentId: cid!,
                    pricePerBytePerSecond: pricePerBytePerSecond,
                    requiredCollateral: 100.TestTokens(),
                    minRequiredNumberOfNodes: 3,
                    proofProbability: 10,
                    duration: contractDuration);

                Assert.That(!string.IsNullOrEmpty(purchaseId));
            }
            finally
            {
                flow.Stop(rc);
            }
        }

        [TestMoment(t: DayThree)]
        public void StoredDataIsAvailableAfterThreeDays()
        {
            var (workflowCreator, lifecycle) = CreateFacilities();
            var flow = workflowCreator.CreateWorkflow();
            var startupConfig = new StartupConfig();
            var codexStartConfig = new CodexStartupConfig(CodexLogLevel.Debug);
            startupConfig.Add(codexStartConfig);
            var rc = flow.Start(1, Location.Unspecified, new CodexContainerRecipe(), startupConfig);

            try
            {
                var container = rc.Containers[0];
                var codexAccess = new CodexAccess(lifecycle, container);

                var result = DownloadContent(codexAccess.Node, cid!);

                file.AssertIsEqual(result);
            }
            finally
            {
                flow.Stop(rc);
            }
        }

        private (WorkflowCreator, TestLifecycle) CreateFacilities()
        {
            var lifecycleConfig = new DistTestCore.Configuration
            (
                kubeConfigFile: Configuration.KubeConfigFile,
                logPath: "null",
            logDebug: false,
                dataFilesPath: "notUsed",
                codexLogLevel: CodexLogLevel.Debug,
                runnerLocation: TestRunnerLocation.InternalToCluster
            );

            var kubeConfig = new KubernetesWorkflow.Configuration(
                k8sNamespacePrefix: MarketplaceTestNamespace,
                kubeConfigFile: Configuration.KubeConfigFile,
                operationTimeout: TimeSet.K8sOperationTimeout(),
            retryDelay: TimeSet.WaitForK8sServiceDelay());

            var workflowCreator = new WorkflowCreator(Log, kubeConfig, testNamespacePostfix: string.Empty);
            var lifecycle = new TestLifecycle(new NullLog(), lifecycleConfig, TimeSet, workflowCreator);

            return (workflowCreator, lifecycle);
        }
    }
}
