using CodexClient;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.MarketTests
{
    [TestFixture(6, 3, 1)]
    [TestFixture(6, 4, 2)]
    [TestFixture(8, 5, 1)]
    [TestFixture(8, 5, 2)]
    [TestFixture(8, 6, 1)]
    [TestFixture(8, 6, 2)]
    [TestFixture(8, 6, 3)]
    [TestFixture(8, 8, 1)]
    [TestFixture(8, 8, 2)]
    [TestFixture(8, 8, 3)]
    [TestFixture(8, 8, 4)]
    public class ContractSuccessfulTest : MarketplaceAutoBootstrapDistTest
    {
        public ContractSuccessfulTest(int hosts, int slots, int tolerance)
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
        protected override TimeSpan HostAvailabilityMaxDuration => Get8TimesConfiguredPeriodDuration();

        [Test]
        public void ContractSuccessful()
        {
            var hosts = StartHosts();
            var client = StartClients().Single();

            var request = CreateStorageRequest(client);

            request.WaitForStorageContractSubmitted();
            AssertContractIsOnChain(request);

            request.WaitForStorageContractStarted();
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
