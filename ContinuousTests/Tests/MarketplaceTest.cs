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
        public override TimeSpan RunTestEvery => TimeSpan.FromMinutes(15);
        public override TestFailMode TestFailMode => TestFailMode.StopAfterFirstFailure;
        public override int EthereumAccountIndex => 200;
        public override string CustomK8sNamespace => "codex-continuous-marketplace";

        private readonly uint numberOfSlots = 3;
        private readonly ByteSize fileSize = 10.MB();
        private readonly TestToken pricePerSlotPerSecond = 10.TestTokens();

        private TestFile file = null!;
        private ContentId? cid;
        private string purchaseId = string.Empty;

        [TestMoment(t: Zero)]
        public void NodePostsStorageRequest()
        {
            var contractDuration = TimeSpan.FromMinutes(11); //TimeSpan.FromDays(3) + TimeSpan.FromHours(1);
            decimal totalDurationSeconds = Convert.ToDecimal(contractDuration.TotalSeconds);
            var expectedTotalCost = numberOfSlots * pricePerSlotPerSecond.Amount * (totalDurationSeconds + 1) * 1000000;

            file = FileManager.GenerateTestFile(fileSize);

            var (workflowCreator, lifecycle) = CreateFacilities();
            var flow = workflowCreator.CreateWorkflow();

            try
            {
                var debugInfo = Nodes[0].GetDebugInfo();
                Assert.That(!string.IsNullOrEmpty(debugInfo.spr));

                var startupConfig = new StartupConfig();
                var codexStartConfig = new CodexStartupConfig(CodexLogLevel.Trace);
                codexStartConfig.MarketplaceConfig = new MarketplaceInitialConfig(0.Eth(), 0.TestTokens(), false);
                codexStartConfig.MarketplaceConfig.AccountIndexOverride = EthereumAccountIndex;
                codexStartConfig.BootstrapSpr = debugInfo.spr;
                startupConfig.Add(codexStartConfig);
                startupConfig.Add(Configuration.CodexDeployment.GethStartResult);
                var rc = flow.Start(1, Location.Unspecified, new CodexContainerRecipe(), startupConfig);

                var account = Configuration.CodexDeployment.GethStartResult.CompanionNode.Accounts[EthereumAccountIndex];
                var tokenAddress = Configuration.CodexDeployment.GethStartResult.MarketplaceNetwork.Marketplace.TokenAddress;

                var interaction = Configuration.CodexDeployment.GethStartResult.MarketplaceNetwork.Bootstrap.StartInteraction(lifecycle);
                interaction.MintTestTokens(new[] { account.Account }, expectedTotalCost, tokenAddress);

                var container = rc.Containers[0];
                var marketplaceNetwork = Configuration.CodexDeployment.GethStartResult.MarketplaceNetwork;
                var codexAccess = new CodexAccess(lifecycle, container);
                var myNodeInfo = codexAccess.Node.GetDebugInfo();

                var marketAccess = new MarketplaceAccess(lifecycle, marketplaceNetwork, account, codexAccess);

                cid = UploadFile(codexAccess.Node, file);
                Assert.That(cid, Is.Not.Null);

                purchaseId = marketAccess.RequestStorage(
                    contentId: cid!,
                    pricePerSlotPerSecond: pricePerSlotPerSecond,
                    requiredCollateral: 100.TestTokens(),
                    minRequiredNumberOfNodes: numberOfSlots,
                    proofProbability: 10,
                    duration: contractDuration);

                Log($"PurchaseId: '{purchaseId}'");
                Assert.That(!string.IsNullOrEmpty(purchaseId));

                var lastState = "";
                var waitStart = DateTime.UtcNow;
                var filesizeInMb = fileSize.SizeInBytes / 1024;
                var maxWaitTime = TimeSpan.FromSeconds(filesizeInMb * 10.0);
                while (lastState != "started")
                {
                    var purchaseStatus = codexAccess.Node.GetPurchaseStatus(purchaseId);
                    if (purchaseStatus != null && purchaseStatus.state != lastState) 
                    {
                        lastState = purchaseStatus.state;
                    }

                    Thread.Sleep(2000);

                    if (lastState == "errored")
                    {
                        Assert.Fail("Contract start failed: " + JsonConvert.SerializeObject(purchaseStatus));
                    }

                    if (DateTime.UtcNow - waitStart > maxWaitTime)
                    {
                        Assert.Fail($"Contract was not picked up within {maxWaitTime.TotalSeconds} seconds timeout: " + JsonConvert.SerializeObject(purchaseStatus));
                    }
                }
            }
            finally
            {
                flow.DeleteTestResources();
            }
        }

        [TestMoment(t: MinuteFive * 2)]
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

            var workflowCreator = new WorkflowCreator(base.Log, kubeFlowConfig, testNamespacePostfix: string.Empty);
            var lifecycle = new TestLifecycle(new NullLog(), lifecycleConfig, TimeSet, workflowCreator);

            return (workflowCreator, lifecycle);
        }

        private string? GetKubeConfig(string kubeConfigFile)
        {
            if (string.IsNullOrEmpty(kubeConfigFile) || kubeConfigFile.ToLowerInvariant() == "null") return null;
            return kubeConfigFile;
        }

        private new void Log(string msg)
        {
            base.Log.Log(msg);
        }
    }
}
