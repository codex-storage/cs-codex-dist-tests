//using DistTestCore;
//using DistTestCore.Codex;
//using Newtonsoft.Json;
//using NUnit.Framework;
//using Utils;

//namespace ContinuousTests.Tests
//{
//    public class MarketplaceTest : ContinuousTest
//    {
//        public override int RequiredNumberOfNodes => 1;
//        public override TimeSpan RunTestEvery => TimeSpan.FromMinutes(10);
//        public override TestFailMode TestFailMode => TestFailMode.StopAfterFirstFailure;
//        public override int EthereumAccountIndex => 200;
//        public override string CustomK8sNamespace => "codex-continuous-marketplace";

//        private readonly uint numberOfSlots = 3;
//        private readonly ByteSize fileSize = 10.MB();
//        private readonly TestToken pricePerSlotPerSecond = 10.TestTokens();

//        private TestFile file = null!;
//        private ContentId? cid;
//        private string purchaseId = string.Empty;

//        [TestMoment(t: Zero)]
//        public void NodePostsStorageRequest()
//        {
//            var contractDuration = TimeSpan.FromMinutes(8);
//            decimal totalDurationSeconds = Convert.ToDecimal(contractDuration.TotalSeconds);
//            var expectedTotalCost = numberOfSlots * pricePerSlotPerSecond.Amount * (totalDurationSeconds + 1) * 1000000;

//            file = FileManager.GenerateTestFile(fileSize);

//            NodeRunner.RunNode((codexAccess, marketplaceAccess) =>
//            {
//                cid = UploadFile(codexAccess.Node, file);
//                Assert.That(cid, Is.Not.Null);

//                purchaseId = marketplaceAccess.RequestStorage(
//                    contentId: cid!,
//                    pricePerSlotPerSecond: pricePerSlotPerSecond,
//                    requiredCollateral: 100.TestTokens(),
//                    minRequiredNumberOfNodes: numberOfSlots,
//                    proofProbability: 10,
//                    duration: contractDuration);

//                Assert.That(!string.IsNullOrEmpty(purchaseId));

//                WaitForContractToStart(codexAccess, purchaseId);
//            });
//        }

//        [TestMoment(t: MinuteFive + MinuteOne)]
//        public void StoredDataIsAvailableAfterThreeDays()
//        {
//            NodeRunner.RunNode((codexAccess, marketplaceAccess) =>
//            {
//                var result = DownloadFile(codexAccess.Node, cid!);

//                file.AssertIsEqual(result);
//            });
//        }

//        private void WaitForContractToStart(CodexAccess codexAccess, string purchaseId)
//        {
//            var lastState = "";
//            var waitStart = DateTime.UtcNow;
//            var filesizeInMb = fileSize.SizeInBytes / (1024 * 1024);
//            var maxWaitTime = TimeSpan.FromSeconds(filesizeInMb * 10.0);

//            Log.Log($"{nameof(WaitForContractToStart)} for {Time.FormatDuration(maxWaitTime)}");
//            while (lastState != "started")
//            {
//                CancelToken.ThrowIfCancellationRequested();

//                var purchaseStatus = codexAccess.Node.GetPurchaseStatus(purchaseId);
//                var statusJson = JsonConvert.SerializeObject(purchaseStatus);
//                if (purchaseStatus != null && purchaseStatus.state != lastState)
//                {
//                    lastState = purchaseStatus.state;
//                    Log.Log("Purchase status: " + statusJson);
//                }

//                Thread.Sleep(2000);

//                if (lastState == "errored")
//                {
//                    Assert.Fail("Contract start failed: " + statusJson);
//                }

//                if (DateTime.UtcNow - waitStart > maxWaitTime)
//                {
//                    Assert.Fail($"Contract was not picked up within {maxWaitTime.TotalSeconds} seconds timeout: {statusJson}");
//                }
//            }
//            Log.Log("Contract started.");
//        }
//    }
//}
