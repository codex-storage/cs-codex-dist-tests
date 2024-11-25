using CodexContractsPlugin;
using CodexPlugin;
using GethPlugin;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.MarketTests
{
    [TestFixture]
    public class ContractSuccessfulTest : MarketplaceAutoBootstrapDistTest
    {
        private const int FilesizeMb = 10;

        protected override int NumberOfHosts => 4;
        protected override int NumberOfClients => 1;
        protected override ByteSize HostAvailabilitySize => (5 * FilesizeMb).MB();
        protected override TimeSpan HostAvailabilityMaxDuration => Get8TimesConfiguredPeriodDuration();
        private readonly TestToken pricePerSlotPerSecond = 10.TstWei();

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

            request.WaitForStorageContractFinished(GetContracts());

            AssertClientHasPaidForContract(pricePerSlotPerSecond, client, request, hosts);
            AssertHostsWerePaidForContract(pricePerSlotPerSecond, request, hosts);
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
                MinRequiredNumberOfNodes = (uint)NumberOfHosts,
                NodeFailureTolerance = (uint)(NumberOfHosts / 2),
                PricePerSlotPerSecond = pricePerSlotPerSecond,
                ProofProbability = 20,
                RequiredCollateral = 1.Tst()
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
            var config = GetContracts().Deployment.Config;
            return TimeSpan.FromSeconds(((double)config.Proofs.Period) * 8.0);
        }
    }
}
