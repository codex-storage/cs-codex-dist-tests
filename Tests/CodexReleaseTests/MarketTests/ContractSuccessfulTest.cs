using CodexContractsPlugin;
using CodexPlugin;
using GethPlugin;
using Nethereum.Hex.HexConvertors.Extensions;
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

            request.WaitForStorageContractFinished(GetContracts());

            AssertClientHasPaidForContract(client, request, hosts);
            AssertHostsWerePaidForContract(request, hosts);
            AssertHostsCollateralsAreUnchanged(hosts);
        }

        private void AssertContractIsOnChain(IStoragePurchaseContract contract)
        {
            AssertOnChainEvents(events =>
            {
                var onChainRequests = events.GetStorageRequests();
                if (onChainRequests.Any(r => r.Id == contract.PurchaseId)) return;
                throw new Exception($"OnChain request {contract.PurchaseId} not found...");
            }, nameof(AssertContractIsOnChain));
        }

        private void AssertContractSlotsAreFilledByHosts(IStoragePurchaseContract contract, ICodexNodeGroup hosts)
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
        }

        private void AssertClientHasPaidForContract(ICodexNode client, IStoragePurchaseContract contract, ICodexNodeGroup hosts)
        {
            var balance = GetTstBalance(client);
            var expectedBalance = StartingBalanceTST.Tst() - GetContractFinalCost(contract, hosts);

            Assert.That(balance, Is.EqualTo(expectedBalance), "Client balance incorrect.");
        }

        private void AssertHostsWerePaidForContract(IStoragePurchaseContract contract, ICodexNodeGroup hosts)
        {
            var fills = GetOnChainSlotFills(hosts);
            var submitUtc = GetContractOnChainSubmittedUtc(contract);
            var finishUtc = submitUtc + contract.Purchase.Duration;
            var expectedBalances = new Dictionary<EthAddress, TestToken>();
            foreach (var host in hosts) expectedBalances.Add(host.EthAddress, StartingBalanceTST.Tst());
            foreach (var fill in fills)
            {
                var slotDuration = finishUtc - fill.SlotFilledEvent.Block.Utc;
                expectedBalances[fill.Host.EthAddress] += GetContractCostPerSlot(slotDuration);
            }

            foreach (var pair in expectedBalances)
            {
                var balance = GetTstBalance(pair.Key);
                Assert.That(balance, Is.EqualTo(pair.Value), "Host was not paid for storage.");
            }
        }

        private void AssertHostsCollateralsAreUnchanged(ICodexNodeGroup hosts)
        {
            // There is no separate collateral location yet.
            // All host balances should be equal to or greater than the starting balance.
            foreach (var host in hosts)
            {
                Assert.That(GetTstBalance(host), Is.GreaterThanOrEqualTo(StartingBalanceTST.Tst()));
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

        private TestToken GetContractFinalCost(IStoragePurchaseContract contract, ICodexNodeGroup hosts)
        {
            var fills = GetOnChainSlotFills(hosts);
            var result = 0.Tst();
            var submitUtc = GetContractOnChainSubmittedUtc(contract);
            var finishUtc = submitUtc + contract.Purchase.Duration;

            foreach (var fill in fills)
            {
                var slotDuration = finishUtc - fill.SlotFilledEvent.Block.Utc;
                result += GetContractCostPerSlot(slotDuration);
            }

            return result;
        }

        private DateTime GetContractOnChainSubmittedUtc(IStoragePurchaseContract contract)
        {
            var events = GetContracts().GetEvents(GetTestRunTimeRange());
            var submitEvent = events.GetStorageRequests().Single(e => e.RequestId.ToHex(false) == contract.PurchaseId);
            return submitEvent.Block.Utc;
        }

        private TestToken GetContractCostPerSlot(TimeSpan slotDuration)
        {
            return PricePerSlotPerSecondTSTWei.TstWei() * (int)slotDuration.TotalSeconds;
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
