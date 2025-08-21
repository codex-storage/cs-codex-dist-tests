using CodexClient;
using CodexContractsPlugin.ChainMonitor;
using CodexContractsPlugin.Marketplace;
using CodexReleaseTests.Utils;
using Nethereum.Hex.HexConvertors.Extensions;
using Newtonsoft.Json;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.MarketTests
{
    [TestFixture]
    public class StabilityTest : MarketplaceAutoBootstrapDistTest, IPeriodMonitorEventHandler
    {
        #region Setup

        private readonly PurchaseParams purchaseParams = new PurchaseParams(
            nodes: 4,
            tolerance: 2,
            uploadFilesize: 32.MB()
        );

        public StabilityTest()
        {
            Assert.That(purchaseParams.Nodes, Is.LessThan(NumberOfHosts));
        }

        protected override int NumberOfHosts => 6;
        protected override int NumberOfClients => 1;
        protected override ByteSize HostAvailabilitySize => purchaseParams.SlotSize.Multiply(1.1); // Each host can hold 1 slot.
        protected override TimeSpan HostAvailabilityMaxDuration => TimeSpan.FromDays(5.0);

        #endregion

        [Test]
        [Combinatorial]
        public void Stability(
            [Values(10, 120)] int minutes)
        {
            Assert.That(HostAvailabilityMaxDuration, Is.GreaterThan(TimeSpan.FromMinutes(minutes * 1.1)));

            GetChainMonitor().PeriodMonitorEventHandler = this;

            StartHosts();
            StartValidator();
            var client = StartClients().Single();
            var purchase = CreateStorageRequest(client, minutes);

            Log($"Contract should remain stable for {minutes} minutes.");
            Thread.Sleep(TimeSpan.FromSeconds(minutes));

            Assert.That(client.GetPurchaseStatus(purchase.PurchaseId)?.State, Is.EqualTo(StoragePurchaseState.Started));
        }

        public void OnPeriodReport(PeriodReport report)
        {
            // For each required proof, there should be a submit call.
            foreach (var required in report.Required)
            {
                var matchingCall = GetMatchingSubmitProofCall(report, required);

                Assert.That(matchingCall.FromAddress.ToLowerInvariant(), Is.EqualTo(required.Host.Address.ToLowerInvariant()));
                Assert.That(matchingCall.Id.ToHex(), Is.EqualTo(required.SlotId.ToHex()));
            }

            // There can't be any calls to mark a proof as missed.
            foreach (var call in report.FunctionCalls)
            {
                var missedCall = nameof(MarkProofAsMissingFunction);
                Assert.That(call.Name, Is.Not.EqualTo(missedCall));
            }
        }

        private SubmitProofFunction GetMatchingSubmitProofCall(PeriodReport report, PeriodRequiredProof required)
        {
            var submitCall = nameof(SubmitProofFunction);
            var call = report.FunctionCalls.SingleOrDefault(f => f.Name == submitCall);
            if (call == null) throw new Exception("Call to submitProof not found for " + required.Describe());
            var callObj = JsonConvert.DeserializeObject<SubmitProofFunction>(call.Payload);
            if (callObj == null) throw new Exception("Unable to deserialize call object");
            return callObj;
        }

        private IStoragePurchaseContract CreateStorageRequest(ICodexNode client, int minutes)
        {
            var cid = client.UploadFile(GenerateTestFile(purchaseParams.UploadFilesize));
            var config = GetContracts().Deployment.Config;
            return client.Marketplace.RequestStorage(new StoragePurchaseRequest(cid)
            {
                Duration = TimeSpan.FromMinutes(minutes) * 1.1,
                Expiry = TimeSpan.FromMinutes(10.0),
                MinRequiredNumberOfNodes = (uint)purchaseParams.Nodes,
                NodeFailureTolerance = (uint)purchaseParams.Tolerance,
                PricePerBytePerSecond = 10.TstWei(),
                ProofProbability = 1, // One proof every period. Free slot as quickly as possible.
                CollateralPerByte = 1.TstWei()
            });
        }
    }
}
