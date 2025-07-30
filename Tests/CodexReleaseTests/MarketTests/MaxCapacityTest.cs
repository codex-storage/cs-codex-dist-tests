using CodexClient;
using CodexReleaseTests.Utils;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.MarketTests
{
    public class MaxCapacityTest : MarketplaceAutoBootstrapDistTest
    {
        private readonly TestToken pricePerBytePerSecond = 10.TstWei();
        private readonly PurchaseParams purchaseParams = new PurchaseParams(
            nodes: 10,
            tolerance: 5,
            uploadFilesize: 10.MB()
        );

        protected override int NumberOfHosts => purchaseParams.Nodes / 2;
        protected override int NumberOfClients => 1;
        protected override ByteSize HostAvailabilitySize => purchaseParams.SlotSize.Multiply(2.1);
        protected override TimeSpan HostAvailabilityMaxDuration => GetContractDuration() * 2;

        [Test]
        [Combinatorial]
        public void TwoSlotsEach(
            [Rerun] int rerun
        )
        {
            var hosts = StartHosts();
            var client = StartClients().Single();
            AssertHostAvailabilitiesAreEmpty(hosts);

            var request = CreateStorageRequest(client);

            request.WaitForStorageContractSubmitted();
            AssertContractIsOnChain(request);

            WaitForContractStarted(request);
            AssertContractSlotsAreFilledByHosts(request, hosts);
        }

        private IStoragePurchaseContract CreateStorageRequest(ICodexNode client)
        {
            var cid = client.UploadFile(GenerateTestFile(purchaseParams.UploadFilesize));
            var config = GetContracts().Deployment.Config;
            return client.Marketplace.RequestStorage(new StoragePurchaseRequest(cid)
            {
                Duration = GetContractDuration(),
                Expiry = GetContractExpiry(),
                MinRequiredNumberOfNodes = (uint)purchaseParams.Nodes,
                NodeFailureTolerance = (uint)purchaseParams.Tolerance,
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
