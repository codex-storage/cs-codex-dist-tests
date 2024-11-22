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
        private const int PricePerSlotPerSecondTSTWei = 10;

        protected override int NumberOfHosts => 4;
        protected override int NumberOfClients => 1;
        protected override ByteSize HostAvailabilitySize => (5 * FilesizeMb).MB();
        protected override TimeSpan HostAvailabilityMaxDuration => GetHostAvailabilityDuration();

        [Test]
        public void ContractSuccessful()
        {
            var hosts = StartHosts();
            var client = StartClients().Single();

            var request = CreateStorageRequest(client);

            request.WaitForStorageContractSubmitted();
            AssertContractIsOnChain(request);

            request.WaitForStorageContractStarted();
            var slotFills = AssertContractSlotsAreFilledByHosts(request, hosts);

            request.WaitForStorageContractFinished();
            //EveryoneWithdrawsFunds(hosts, client);

            GetContracts().WithdrawFunds(request.PurchaseId, client.EthAddress);
            foreach (var host in hosts)
                GetContracts().WithdrawFunds(request.PurchaseId, host.EthAddress);

            AssertClientHasPaidForContract(client, request);
            AssertHostsWerePaidForContract(request, hosts, slotFills);
            AssertHostsCollateralsAreUnchanged(hosts);
        }

        //private void EveryoneWithdrawsFunds(ICodexNodeGroup hosts, ICodexNode client)
        //{
        //    foreach (var host in hosts) host.Marketplace.WithdrawFunds();
        //    client.Marketplace.WithdrawFunds();
        //}

        private void AssertContractIsOnChain(IStoragePurchaseContract contract)
        {
            AssertOnChainEvents(events =>
            {
                var onChainRequests = events.GetStorageRequests();
                if (onChainRequests.Any(r => r.Id == contract.PurchaseId)) return;
                throw new Exception($"OnChain request {contract.PurchaseId} not found...");
            }, nameof(AssertContractIsOnChain));
        }

        private SlotFill[] AssertContractSlotsAreFilledByHosts(IStoragePurchaseContract contract, ICodexNodeGroup hosts)
        {
            var activeHosts = new Dictionary<int, SlotFill>();

            Time.Retry(() =>
            {
                var fills = GetOnChainSlotFills(hosts, contract.PurchaseId);
                foreach (var fill in fills)
                {
                    var index = (int)fill.SlotFilledEvent.SlotIndex;
                    if (!activeHosts.ContainsKey(index))
                    {
                        activeHosts.Add(index, fill);
                    }
                }

                if (activeHosts.Count != contract.Purchase.MinRequiredNumberOfNodes) throw new Exception("Not all slots were filled...");

            }, nameof(AssertContractSlotsAreFilledByHosts));

            return activeHosts.Values.ToArray();
        }

        private void AssertClientHasPaidForContract(ICodexNode client, IStoragePurchaseContract contract)
        {
            var balance = GetContracts().GetTestTokenBalance(client);
            var expectedBalance = StartingBalanceTST.Tst() - GetContractTotalCost();

            Assert.That(balance, Is.EqualTo(expectedBalance), "Client balance incorrect.");
        }

        private void AssertHostsWerePaidForContract(IStoragePurchaseContract contract, ICodexNodeGroup hosts, SlotFill[] fills)
        {
            var expectedBalances = new Dictionary<EthAddress, TestToken>();
            foreach (var host in hosts) expectedBalances.Add(host.EthAddress, StartingBalanceTST.Tst());
            foreach (var fill in fills)
            {
                expectedBalances[fill.Host.EthAddress] += GetContractCostPerSlot();
            }

            foreach (var pair in expectedBalances)
            {
                var balance = GetContracts().GetTestTokenBalance(pair.Key);
                Assert.That(balance, Is.EqualTo(pair.Value), "Host was not paid for storage.");
            }
        }

        private void AssertHostsCollateralsAreUnchanged(ICodexNodeGroup hosts)
        {
            // There is no separate collateral location yet.
            // All host balances should be equal to or greater than the starting balance.
            foreach (var host in hosts)
            {
                Assert.That(GetContracts().GetTestTokenBalance(host), Is.GreaterThanOrEqualTo(StartingBalanceTST.Tst()));
            }
        }

        private void AssertOnChainEvents(Action<ICodexContractsEvents> onEvents, string description)
        {
            Time.Retry(() =>
            {
                var events = GetContracts().GetEvents(GetTestRunTimeRange());
                onEvents(events);
            }, description);
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
                PricePerSlotPerSecond = PricePerSlotPerSecondTSTWei.TstWei(),
                ProofProbability = 20,
                RequiredCollateral = 1.Tst()
            });
        }

        private TestToken GetContractTotalCost()
        {
            return GetContractCostPerSlot() * NumberOfHosts;
        }

        private TestToken GetContractCostPerSlot()
        {
            var duration = GetContractDuration();
            return PricePerSlotPerSecondTSTWei.TstWei() * ((int)duration.TotalSeconds);
        }

        private TimeSpan GetContractExpiry()
        {
            return GetContractDuration() / 2;
        }

        private TimeSpan GetContractDuration()
        {
            return GetHostAvailabilityDuration() / 2;
        }

        private TimeSpan GetHostAvailabilityDuration()
        {
            var config = GetContracts().Deployment.Config;
            return TimeSpan.FromSeconds(((double)config.Proofs.Period) * 8.0);
        }
    }
}
