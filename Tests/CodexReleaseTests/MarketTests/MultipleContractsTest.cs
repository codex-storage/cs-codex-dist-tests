using CodexClient;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.MarketTests
{
    [TestFixture]
    public class MultipleContractsTest : MarketplaceAutoBootstrapDistTest
    {
        private const int FilesizeMb = 10;

        protected override int NumberOfHosts => 8;
        protected override int NumberOfClients => 3;
        protected override ByteSize HostAvailabilitySize => (5 * FilesizeMb).MB();
        protected override TimeSpan HostAvailabilityMaxDuration => Get8TimesConfiguredPeriodDuration();
        private readonly TestToken pricePerBytePerSecond = 10.TstWei();

        [Test]
        [Ignore("TODO - Test where multiple successful contracts are run simultaenously")]
        public void MultipleSuccessfulContracts()
        {
            var hosts = StartHosts();
            var clients = StartClients();

            var requests = clients.Select(c => CreateStorageRequest(c)).ToArray();

            All(requests, r =>
            {
                r.WaitForStorageContractSubmitted();
                AssertContractIsOnChain(r);
            });

            All(requests, r => r.WaitForStorageContractStarted());
            All(requests, r => AssertContractSlotsAreFilledByHosts(r, hosts));

            All(requests, r => r.WaitForStorageContractFinished());

                // todo: removed from codexclient:
                //contracts.WaitUntilNextPeriod();
                //contracts.WaitUntilNextPeriod();

                //var blocks = 3;
                //Log($"Waiting {blocks} blocks for nodes to process payouts...");
                //Thread.Sleep(GethContainerRecipe.BlockInterval * blocks);

            // todo:
            //AssertClientHasPaidForContract(pricePerSlotPerSecond, client, request, hosts);
            //AssertHostsWerePaidForContract(pricePerSlotPerSecond, request, hosts);
            //AssertHostsCollateralsAreUnchanged(hosts);
        }

        private void All(IStoragePurchaseContract[] requests, Action<IStoragePurchaseContract> action)
        {
            foreach (var r in requests) action(r);
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
                PricePerBytePerSecond = pricePerBytePerSecond,
                ProofProbability = 20,
                CollateralPerByte = 1.Tst()
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
