using CodexClient;
using CodexPlugin;
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
        [Ignore("TODO - wip")]
        [Combinatorial]
        public void MultipleContractGenerations(
            [Values(1, 5, 10)] int numGenerations)
        {
            var hosts = StartHosts();

            for (var i = 0; i < numGenerations; i++)
            {
                Log("Generation: " + i);
                Generation(hosts);
            }
        }

        private void Generation(ICodexNodeGroup hosts)
        {
            var clients = StartClients();

            var requests = clients.Select(CreateStorageRequest).ToArray();

            All(requests, r =>
            {
                r.WaitForStorageContractSubmitted();
                AssertContractIsOnChain(r);
            });

            All(requests, r => r.WaitForStorageContractStarted());
            All(requests, r => AssertContractSlotsAreFilledByHosts(r, hosts));
            All(requests, r => r.WaitForStorageContractFinished());
        }

        private void All(IStoragePurchaseContract[] requests, Action<IStoragePurchaseContract> action)
        {
            var tasks = requests.Select(r => Task.Run(() => action(r))).ToArray();
            Task.WaitAll(tasks);
            foreach(var t in tasks)
            {
                if (t.Exception != null) throw t.Exception;
            }
        }

        private IStoragePurchaseContract CreateStorageRequest(ICodexNode client)
        {
            var cid = client.UploadFile(GenerateTestFile(FilesizeMb.MB()));
            var config = GetContracts().Deployment.Config;
            return client.Marketplace.RequestStorage(new StoragePurchaseRequest(cid)
            {
                Duration = GetContractDuration(),
                Expiry = GetContractExpiry(),
                MinRequiredNumberOfNodes = (uint)NumberOfHosts / 2,
                NodeFailureTolerance = (uint)(NumberOfHosts / 4),
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
