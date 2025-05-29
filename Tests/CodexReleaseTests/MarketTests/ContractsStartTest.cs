using CodexClient;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.MarketTests
{
    [TestFixture]
    public class ContractsStartTest : MarketplaceAutoBootstrapDistTest
    {
        private const int FilesizeMb = 10;
        private readonly TestToken pricePerBytePerSecond = 10.TstWei();

        protected override int NumberOfHosts => 5;
        protected override int NumberOfClients => 1;
        protected override ByteSize HostAvailabilitySize => (5 * FilesizeMb).MB();
        protected override TimeSpan HostAvailabilityMaxDuration => Get8TimesConfiguredPeriodDuration() * 12;

        [Test]
        [Combinatorial]
        public void ContractStarts(
            [Values(1, 2, 3)] int rerunA,
            [Values(1, 2, 3)] int rerunB,
            [Values(1, 2, 3)] int rerunC)
        {
            var hosts = StartHosts();
            var client = StartClients().Single();

            var request = CreateStorageRequest(client);

            request.WaitForStorageContractSubmitted();
            AssertContractIsOnChain(request);

            WaitForContractStarted(request);
            AssertContractSlotsAreFilledByHosts(request, hosts);
        }

        private IStoragePurchaseContract CreateStorageRequest(ICodexNode client)
        {
            var cid = client.UploadFile(GenerateTestFile(FilesizeMb.MB()));
            var config = GetContracts().Deployment.Config;
            return client.Marketplace.RequestStorage(new StoragePurchaseRequest(cid)
            {
                Duration = GetContractDuration(),
                Expiry = GetContractExpiry(),
                MinRequiredNumberOfNodes = 3,
                NodeFailureTolerance = 1,
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
            return Get8TimesConfiguredPeriodDuration();
        }

        private TimeSpan Get8TimesConfiguredPeriodDuration()
        {
            return GetPeriodDuration() * 8.0;
        }
    }
}
