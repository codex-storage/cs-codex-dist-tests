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
    public class StabilityTest : MarketplaceAutoBootstrapDistTest
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

        private int numPeriods = 0;
        private bool proofWasMissed = false;

        [Test]
        [Combinatorial]
        public void Stability(
            [Values(10, 120)] int minutes)
        {
            var mins = TimeSpan.FromMinutes(minutes);
            var periodDuration = GetContracts().Deployment.Config.PeriodDuration;
            Assert.That(HostAvailabilityMaxDuration, Is.GreaterThan(mins * 1.1));

            numPeriods = 0;
            proofWasMissed = false;

            StartHosts();
            StartValidator();
            var client = StartClients().Single();
            var purchase = CreateStorageRequest(client, mins);
            purchase.WaitForStorageContractStarted();

            Log($"Contract should remain stable for {Time.FormatDuration(mins)}.");
            var endUtc = DateTime.UtcNow + mins;
            while (DateTime.UtcNow < endUtc)
            {
                Thread.Sleep(TimeSpan.FromSeconds(10));
                if (proofWasMissed)
                {
                    // We wait because we want to log calls to MarkProofAsMissing.
                    Thread.Sleep(periodDuration * 1.1);
                    Assert.Fail("Proof was missed.");
                }
            }

            var minNumPeriod = (mins / periodDuration) - 1.0;
            Log($"{numPeriods} periods elapsed. Expected at least {minNumPeriod} periods.");
            Assert.That(numPeriods, Is.GreaterThanOrEqualTo(minNumPeriod));

            var status = client.GetPurchaseStatus(purchase.PurchaseId);
            if (status == null) throw new Exception("Purchase status not found");
            Assert.That(status.IsStarted || status.IsFinished);
        }

        protected override void OnPeriod(PeriodReport report)
        {
            numPeriods++;

            // For each required proof, there should be a submit call.
            var calls = GetSubmitProofCalls(report);
            foreach (var required in report.Required)
            {
                var matchingCall = GetMatchingSubmitProofCall(calls, required);
                if (matchingCall == null)
                {
                    Log($"A proof was missed for {required.Describe()}. Failing test after a delay so chain events have time to log...");
                    proofWasMissed = true;
                }
            }

            // There can't be any calls to mark a proof as missed.
            foreach (var call in report.FunctionCalls)
            {
                var missedCall = nameof(MarkProofAsMissingFunction);
                Assert.That(call.Name, Is.Not.EqualTo(missedCall));
            }
        }

        private SubmitProofFunction? GetMatchingSubmitProofCall(SubmitProofFunction[] calls, PeriodRequiredProof required)
        {
            foreach (var call in calls)
            {
                if (
                    call.Id.SequenceEqual(required.SlotId) &&
                    call.FromAddress.ToLowerInvariant() == required.Host.Address.ToLowerInvariant()
                )
                {
                    return call;
                }
            }

            return null;
        }

        private SubmitProofFunction[] GetSubmitProofCalls(PeriodReport report)
        {
            var submitCall = nameof(SubmitProofFunction);
            var calls = report.FunctionCalls.Where(f => f.Name == submitCall).ToArray();
            var callObjs = calls.Select(call => JsonConvert.DeserializeObject<SubmitProofFunction>(call.Payload)).ToArray();
            Log($"SubmitProof calls: {callObjs.Length}");
            foreach (var c in callObjs)
            {
                Log($" - slotId:{c.Id.ToHex()} host:{c.FromAddress}");
            }

            return callObjs!;
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
