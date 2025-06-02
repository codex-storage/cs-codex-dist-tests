using CodexClient;
using CodexReleaseTests.Utils;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.MarketTests
{
    public class FailTest : MarketplaceAutoBootstrapDistTest
    {
        protected override int NumberOfHosts => 4;
        protected override int NumberOfClients => 1;
        protected override ByteSize HostAvailabilitySize => 1.GB();
        protected override TimeSpan HostAvailabilityMaxDuration => TimeSpan.FromDays(1.0);

        [Ignore("Slots are never freed because proofs are never marked as missing. Issue: https://github.com/codex-storage/nim-codex/issues/1153")]
        [Test]
        [Combinatorial]
        public void Fail(
            [Values([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16])] int rerun
        )
        {
            var hosts = StartHosts();
            var client = StartClients().Single();
            StartValidator();

            var request = CreateStorageRequest(client);

            request.WaitForStorageContractSubmitted();
            AssertContractIsOnChain(request);

            request.WaitForStorageContractStarted();
            AssertContractSlotsAreFilledByHosts(request, hosts);

            hosts.Stop(waitTillStopped: true);

            WaitForSlotFreedEvents();

            request.WaitForContractFailed();
        }

        private void WaitForSlotFreedEvents()
        {
            var start = DateTime.UtcNow;
            var timeout = CalculateContractFailTimespan();

            Log($"{nameof(WaitForSlotFreedEvents)} timeout: {Time.FormatDuration(timeout)}");

            while (DateTime.UtcNow < start + timeout)
            {
                var events = GetContracts().GetEvents(GetTestRunTimeRange());
                var slotFreed = events.GetSlotFreedEvents();
                if (slotFreed.Length == NumberOfHosts)
                {
                    Log($"{nameof(WaitForSlotFreedEvents)} took {Time.FormatDuration(DateTime.UtcNow - start)}");
                    return;
                }
                GetContracts().WaitUntilNextPeriod();
            }
            Assert.Fail($"{nameof(WaitForSlotFreedEvents)} failed after {Time.FormatDuration(timeout)}");
        }

        private IStoragePurchaseContract CreateStorageRequest(ICodexNode client)
        {
            var cid = client.UploadFile(GenerateTestFile(5.MB()));
            return client.Marketplace.RequestStorage(new StoragePurchaseRequest(cid)
            {
                Duration = HostAvailabilityMaxDuration / 2,
                Expiry = TimeSpan.FromMinutes(5.0),
                MinRequiredNumberOfNodes = (uint)NumberOfHosts,
                NodeFailureTolerance = (uint)(NumberOfHosts / 2),
                PricePerBytePerSecond = 100.TstWei(),
                ProofProbability = 1, // Require a proof every period
                CollateralPerByte = 1.TstWei()
            });
        }
    }
}
