using CodexClient;
using CodexPlugin;
using CodexReleaseTests.Utils;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.MarketTests
{
    [TestFixture(10, 20, 5)]
    public class SequentialContracts : MarketplaceAutoBootstrapDistTest
    {
        public SequentialContracts(int hosts, int slots, int tolerance)
        {
            this.hosts = hosts;
            purchaseParams = new PurchaseParams(slots, tolerance, 10.MB());
        }

        private readonly int hosts;
        private readonly PurchaseParams purchaseParams;

        protected override int NumberOfHosts => hosts;
        protected override int NumberOfClients => 6;
        protected override ByteSize HostAvailabilitySize => purchaseParams.SlotSize.Multiply(100.0);
        protected override TimeSpan HostAvailabilityMaxDuration => Get8TimesConfiguredPeriodDuration() * 12;
        private readonly TestToken pricePerBytePerSecond = 10.TstWei();

        [Test]
        [Combinatorial]
        public void Sequential(
            [Values(10)] int numGenerations)
        {
            var hosts = StartHosts();
            var clients = StartClients();

            for (var i = 0; i < numGenerations; i++)
            {
                Log("Generation: " + i);
                try
                {
                    Generation(clients, hosts);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Failed at generation {i} with exception {ex}");
                }
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
            var cid = client.UploadFile(GenerateTestFile(purchaseParams.UploadFilesize));
            var config = GetContracts().Deployment.Config;
            return client.Marketplace.RequestStorage(new StoragePurchaseRequest(cid)
            {
                Duration = GetContractDuration(),
                Expiry = GetContractExpiry(),
                MinRequiredNumberOfNodes = (uint)purchaseParams.Nodes,
                NodeFailureTolerance = (uint)purchaseParams.Tolerance,
                PricePerBytePerSecond = pricePerBytePerSecond,
                ProofProbability = 10000,
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
            return TimeSpan.FromSeconds(config.Proofs.Period * 8.0);
        }
    }
}
