using CodexClient;
using CodexReleaseTests.Utils;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.MarketTests
{
    public class FailTest : MarketplaceAutoBootstrapDistTest
    {
        protected override int NumberOfHosts => 4;
        private readonly int SlotTolerance;
        protected override int NumberOfClients => 1;
        protected override ByteSize HostAvailabilitySize => 1.GB();
        protected override TimeSpan HostAvailabilityMaxDuration => TimeSpan.FromDays(1.0);

        public FailTest()
        {
            SlotTolerance = NumberOfHosts / 2;
        }

        [Test]
        [Combinatorial]
        public void Fail(
            [Rerun] int rerun
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

            var config = GetContracts().Deployment.Config;
            request.WaitForContractFailed(config);
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
                Log($"SlotFreed events: {slotFreed.Length} - Expected: {SlotTolerance}");
                if (slotFreed.Length > SlotTolerance)
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
            var cid = client.UploadFile(GenerateTestFile(3.MB()));
            return client.Marketplace.RequestStorage(new StoragePurchaseRequest(cid)
            {
                Duration = HostAvailabilityMaxDuration / 2,
                Expiry = TimeSpan.FromMinutes(5.0),
                MinRequiredNumberOfNodes = (uint)NumberOfHosts,
                NodeFailureTolerance = (uint)SlotTolerance,
                PricePerBytePerSecond = 100.TstWei(),
                ProofProbability = 1, // Require a proof every period
                CollateralPerByte = 1.TstWei()
            });
        }
    }
}
