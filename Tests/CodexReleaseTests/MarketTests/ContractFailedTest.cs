using CodexClient;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.MarketTests
{
    public class ContractFailedTest : MarketplaceAutoBootstrapDistTest
    {
        protected override int NumberOfHosts => 4;
        protected override int NumberOfClients => 1;
        protected override ByteSize HostAvailabilitySize => 1.GB();
        protected override TimeSpan HostAvailabilityMaxDuration => TimeSpan.FromDays(1.0);
        private readonly TestToken pricePerBytePerSecond = 10.TstWei();

        [Test]
        [Ignore("Disabled for now: Test is unstable.")]
        public void ContractFailed()
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
            Log(nameof(WaitForSlotFreedEvents));

            var start = DateTime.UtcNow;
            var timeout = CalculateContractFailTimespan();

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
                Duration = TimeSpan.FromHours(1.0),
                Expiry = TimeSpan.FromHours(0.2),
                MinRequiredNumberOfNodes = (uint)NumberOfHosts,
                NodeFailureTolerance = (uint)(NumberOfHosts / 2),
                PricePerBytePerSecond = pricePerBytePerSecond,
                ProofProbability = 1, // Require a proof every period
                CollateralPerByte = 1.Tst()
            });
        }
    }
}
