using CodexClient;
using CodexReleaseTests.Utils;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.MarketTests
{
    [TestFixture(5, 3, 1)]
    [TestFixture(10, 20, 10)]
    public class FinishTest : MarketplaceAutoBootstrapDistTest
    {
        public FinishTest(int hosts, int slots, int tolerance)
        {
            this.hosts = hosts;
            this.slots = slots;
            this.tolerance = tolerance;
        }

        private const int FilesizeMb = 10;
        private readonly TestToken pricePerBytePerSecond = 10.TstWei();
        private readonly int hosts;
        private readonly int slots;
        private readonly int tolerance;

        protected override int NumberOfHosts => hosts;
        protected override int NumberOfClients => 1;
        protected override ByteSize HostAvailabilitySize => (5 * FilesizeMb).MB();
        protected override TimeSpan HostAvailabilityMaxDuration => Get8TimesConfiguredPeriodDuration() * 12;

        [Test]
        [Combinatorial]
        public void Finish(
            [Values([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16])] int rerun
        )
        {
            var hosts = StartHosts();
            var client = StartClients().Single();

            var request = CreateStorageRequest(client);

            request.WaitForStorageContractSubmitted();
            AssertContractIsOnChain(request);

            WaitForContractStarted(request);
            AssertContractSlotsAreFilledByHosts(request, hosts);

            request.WaitForStorageContractFinished();

            AssertClientHasPaidForContract(pricePerBytePerSecond, client, request, hosts);
            AssertHostsWerePaidForContract(pricePerBytePerSecond, request, hosts);
            AssertHostsCollateralsAreUnchanged(hosts);
        }

        private IStoragePurchaseContract CreateStorageRequest(ICodexNode client)
        {
            var cid = client.UploadFile(GenerateTestFile(FilesizeMb.MB()));
            var config = GetContracts().Deployment.Config;
            return client.Marketplace.RequestStorage(new StoragePurchaseRequest(cid)
            {
                Duration = GetContractDuration(),
                Expiry = GetContractExpiry(),
                MinRequiredNumberOfNodes = (uint)slots,
                NodeFailureTolerance = (uint)tolerance,
                PricePerBytePerSecond = pricePerBytePerSecond,
                ProofProbability = 20,
                CollateralPerByte = 100.TstWei()
            });
        }

        private TimeSpan GetContractExpiry()
        {
            return GetContractDuration() / 2;
        }

        private TimeSpan GetContractDuration()
        {
            return Get8TimesConfiguredPeriodDuration() / 2;
        }

        private TimeSpan Get8TimesConfiguredPeriodDuration()
        {
            return GetPeriodDuration() * 8.0;
        }
    }
}
