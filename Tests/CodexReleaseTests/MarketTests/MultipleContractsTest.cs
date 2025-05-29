using CodexClient;
using CodexPlugin;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.MarketTests
{
    [TestFixture(12, 48, 12)]
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
        protected override int NumberOfClients => 8;
        protected override ByteSize HostAvailabilitySize => (1000 * FilesizeMb).MB();
        protected override TimeSpan HostAvailabilityMaxDuration => Get8TimesConfiguredPeriodDuration() * 12;
        private readonly TestToken pricePerBytePerSecond = 10.TstWei();

        [Test]
        [Combinatorial]
        public void MultipleContractGenerations(
            [Values(10)] int numGenerations)
        {
            var hosts = StartHosts();
            var clients = StartClients();

            for (var i = 0; i < numGenerations; i++)
            {
                Log("Generation: " + i);
                Generation(clients, hosts);
            }

            Thread.Sleep(TimeSpan.FromSeconds(12.0));
        }

        private void Generation(ICodexNodeGroup clients, ICodexNodeGroup hosts)
        {
            var requests = All(clients.ToArray(), CreateStorageRequest);

            All(requests, r =>
            {
                r.WaitForStorageContractSubmitted();
                AssertContractIsOnChain(r);
            });

            All(requests, WaitForContractStarted);
        }

        private void All<T>(T[] items, Action<T> action)
        {
            var tasks = items.Select(r => Task.Run(() => action(r))).ToArray();
            Task.WaitAll(tasks);
            foreach(var t in tasks)
            {
                if (t.Exception != null) throw t.Exception;
            }
        }

        private TResult[] All<T, TResult>(T[] items, Func<T, TResult> action)
        {
            var tasks = items.Select(r => Task.Run(() => action(r))).ToArray();
            Task.WaitAll(tasks);
            foreach (var t in tasks)
            {
                if (t.Exception != null) throw t.Exception;
            }
            return tasks.Select(t => t.Result).ToArray();
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
                ProofProbability = 1000,
                CollateralPerByte = 1.TstWei()
            });
        }

        private TimeSpan GetContractExpiry()
        {
            return GetContractDuration() / 2;
        }

        private TimeSpan GetContractDuration()
        {
            return Get8TimesConfiguredPeriodDuration() * 4;
        }

        private TimeSpan Get8TimesConfiguredPeriodDuration()
        {
            var config = GetContracts().Deployment.Config;
            return TimeSpan.FromSeconds(((double)config.Proofs.Period) * 8.0);
        }
    }
}
