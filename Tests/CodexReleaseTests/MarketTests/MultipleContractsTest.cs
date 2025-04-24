using CodexClient;
using CodexPlugin;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.MarketTests
{
    [TestFixture(6, 3, 1)]
    [TestFixture(6, 4, 1)]
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
    public class MultipleContractsTest : MarketplaceAutoBootstrapDistTest
    {
        public MultipleContractsTest(int hosts, int slots, int tolerance)
        {
            this.hosts = hosts;
            this.slots = slots;
            this.tolerance = tolerance;
        }

        private const int FilesizeMb = 10;
        private readonly int hosts;
        private readonly int slots;
        private readonly int tolerance;

        protected override int NumberOfHosts => hosts;
        protected override int NumberOfClients => 3;
        protected override ByteSize HostAvailabilitySize => (100 * FilesizeMb).MB();
        protected override TimeSpan HostAvailabilityMaxDuration => Get8TimesConfiguredPeriodDuration() * 3;
        private readonly TestToken pricePerBytePerSecond = 10.TstWei();

        [Test]
        [Combinatorial]
        public void MultipleContractGenerations(
            [Values(5)] int numGenerations)
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

            Thread.Sleep(TimeSpan.FromSeconds(12.0));
            clients.Stop(waitTillStopped: false);

            // for the time being, we're only interested in whether these contracts start.
            //All(requests, r => AssertContractSlotsAreFilledByHosts(r, hosts));
            //All(requests, r => r.WaitForStorageContractFinished());
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
                MinRequiredNumberOfNodes = (uint)slots,
                NodeFailureTolerance = (uint)tolerance,
                PricePerBytePerSecond = pricePerBytePerSecond,
                ProofProbability = 20,
                CollateralPerByte = 1.TstWei()
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
            var config = GetContracts().Deployment.Config;
            return TimeSpan.FromSeconds(((double)config.Proofs.Period) * 8.0);
        }
    }
}
