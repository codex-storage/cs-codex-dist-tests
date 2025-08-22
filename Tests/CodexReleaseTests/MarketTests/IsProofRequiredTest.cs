using CodexClient;
using CodexContractsPlugin.ChainMonitor;
using CodexReleaseTests.Utils;
using Nethereum.Hex.HexConvertors.Extensions;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.MarketTests
{
    [TestFixture]
    public class IsProofRequiredTest : MarketplaceAutoBootstrapDistTest
    {
        #region Setup

        private readonly PurchaseParams purchaseParams = new PurchaseParams(
            nodes: 4,
            tolerance: 2,
            uploadFilesize: 32.MB()
        );

        public IsProofRequiredTest()
        {
            Assert.That(purchaseParams.Nodes, Is.LessThan(NumberOfHosts));
        }

        protected override int NumberOfHosts => 6;
        protected override int NumberOfClients => 1;
        protected override ByteSize HostAvailabilitySize => purchaseParams.SlotSize.Multiply(1.1); // Each host can hold 1 slot.
        protected override TimeSpan HostAvailabilityMaxDuration => TimeSpan.FromDays(5.0);

        #endregion

        [Test]
        public void IsProofRequired()
        {
            var mins = TimeSpan.FromMinutes(10.0);

            StartHosts();
            StartValidator();
            var client = StartClients().Single();
            var purchase = CreateStorageRequest(client, mins);
            purchase.WaitForStorageContractStarted();

            var requestId = purchase.PurchaseId.HexToByteArray();
            var numSlots = purchaseParams.Nodes;
            //var map = new Dictionary<ulong, List<int>>();

            Log($"Checking IsProofRequired every second for {Time.FormatDuration(mins)}.");
            var endUtc = DateTime.UtcNow + mins;
            while (DateTime.UtcNow < endUtc)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
                var requiredSlotIndices = new List<int>();
                for (var i = 0; i < numSlots; i++)
                {
                    if (GetContracts().IsProofRequired(requestId, i)) requiredSlotIndices.Add(i);
                }

                var periodNumber = GetContracts().GetPeriodNumber(DateTime.UtcNow);
                var blockNumber = GetGeth().GetSyncedBlockNumber();
                Log($"[{blockNumber?.ToString().PadLeft(4, '0')}]" +
                    $"{periodNumber.ToString().PadLeft(12, '0')} => " +
                    $"{string.Join(",", requiredSlotIndices.Select(i => i.ToString()))}");

                //var num = currentPeriod.PeriodNumber;
                //if (!map.ContainsKey(num))
                //{
                //    map.Add(num, requiredSlotIndices);
                //    Log($"Period {num} = required proof for slot indices {string.Join(",", requiredSlotIndices.Select(i => i.ToString()))}");
                //}
                //else
                //{
                //    var a = map[num];
                //    CollectionAssert.AreEquivalent(a, requiredSlotIndices);
                //}
            }
        }

        private IStoragePurchaseContract CreateStorageRequest(ICodexNode client, TimeSpan minutes)
        {
            var cid = client.UploadFile(GenerateTestFile(purchaseParams.UploadFilesize));
            var config = GetContracts().Deployment.Config;
            return client.Marketplace.RequestStorage(new StoragePurchaseRequest(cid)
            {
                Duration = minutes * 1.1,
                Expiry = TimeSpan.FromMinutes(8.0),
                MinRequiredNumberOfNodes = (uint)purchaseParams.Nodes,
                NodeFailureTolerance = (uint)purchaseParams.Tolerance,
                PricePerBytePerSecond = 10.TstWei(),
                ProofProbability = 1, // One proof every period. Free slot as quickly as possible.
                CollateralPerByte = 1.TstWei()
            });
        }
    }
}
