using CodexContractsPlugin;
using CodexPlugin;
using FileUtils;
using NUnit.Framework;
using Utils;


/// manual test locally.


namespace ContinuousTests.Tests
{
    public class MarketplaceTest : ContinuousTest
    {
        public override int RequiredNumberOfNodes => 1;
        public override TimeSpan RunTestEvery => TimeSpan.FromMinutes(10);
        public override TestFailMode TestFailMode => TestFailMode.StopAfterFirstFailure;

        private readonly uint numberOfSlots = 3;
        private readonly ByteSize fileSize = 10.MB();
        private readonly TestToken pricePerSlotPerSecond = 10.TestTokens();

        private TrackedFile file = null!;
        private ContentId? cid;
        private string purchaseId = string.Empty;

        [TestMoment(t: Zero)]
        public void NodePostsStorageRequest()
        {
            var contractDuration = TimeSpan.FromMinutes(8);
            decimal totalDurationSeconds = Convert.ToDecimal(contractDuration.TotalSeconds);
            var expectedTotalCost = numberOfSlots * pricePerSlotPerSecond.Amount * (totalDurationSeconds + 1) * 1000000;

            file = FileManager.GenerateFile(fileSize);

            NodeRunner.RunNode(
                s => s.WithName("Buyer"),
                node =>
            {
                cid = node.UploadFile(file);
                Assert.That(cid, Is.Not.Null);

                purchaseId = node.Marketplace.RequestStorage(
                    contentId: cid!,
                    pricePerSlotPerSecond: pricePerSlotPerSecond,
                    requiredCollateral: 100.TestTokens(),
                    minRequiredNumberOfNodes: numberOfSlots,
                    proofProbability: 10,
                    duration: contractDuration);

                Assert.That(!string.IsNullOrEmpty(purchaseId));

                WaitForContractToStart(codexAccess, purchaseId);
            });
        }

        [TestMoment(t: MinuteFive + MinuteOne)]
        public void StoredDataIsAvailableAfterThreeDays()
        {
            NodeRunner.RunNode((codexAccess, marketplaceAccess) =>
            {
                var result = DownloadFile(codexAccess.Node, cid!);

                file.AssertIsEqual(result);
            });
        }
    }
}
