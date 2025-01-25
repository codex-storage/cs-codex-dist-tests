using CodexClient;
using CodexContractsPlugin;
using CodexContractsPlugin.Marketplace;
using CodexPlugin;
using CodexTests;
using DistTestCore;
using GethPlugin;
using Nethereum.Hex.HexConvertors.Extensions;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.MarketTests
{
    public abstract class MarketplaceAutoBootstrapDistTest : AutoBootstrapDistTest
    {
        private readonly Dictionary<TestLifecycle, MarketplaceHandle> handles = new Dictionary<TestLifecycle, MarketplaceHandle>();
        protected const int StartingBalanceTST = 1000;
        protected const int StartingBalanceEth = 10;

        protected override void LifecycleStart(TestLifecycle lifecycle)
        {
            base.LifecycleStart(lifecycle);
            var geth = StartGethNode(s => s.IsMiner());
            var contracts = Ci.StartCodexContracts(geth);
            handles.Add(lifecycle, new MarketplaceHandle(geth, contracts));
        }

        protected override void LifecycleStop(TestLifecycle lifecycle, DistTestResult result)
        {
            base.LifecycleStop(lifecycle, result);
            handles.Remove(lifecycle);
        }

        protected IGethNode GetGeth()
        {
            return handles[Get()].Geth;
        }

        protected ICodexContracts GetContracts()
        {
            return handles[Get()].Contracts;
        }

        protected TimeSpan GetPeriodDuration()
        {
            var config = GetContracts().Deployment.Config;
            return TimeSpan.FromSeconds(((double)config.Proofs.Period));
        }

        protected abstract int NumberOfHosts { get; }
        protected abstract int NumberOfClients { get; }
        protected abstract ByteSize HostAvailabilitySize { get; }
        protected abstract TimeSpan HostAvailabilityMaxDuration { get; }

        public ICodexNodeGroup StartHosts()
        {
            var hosts = StartCodex(NumberOfHosts, s => s
                .WithName("host")
                .EnableMarketplace(GetGeth(), GetContracts(), m => m
                    .WithInitial(StartingBalanceEth.Eth(), StartingBalanceTST.Tst())
                    .AsStorageNode()
                )
            );

            var config = GetContracts().Deployment.Config;
            foreach (var host in hosts)
            {
                Assert.That(GetTstBalance(host).TstWei, Is.EqualTo(StartingBalanceTST.Tst().TstWei));
                Assert.That(GetEthBalance(host).Wei, Is.EqualTo(StartingBalanceEth.Eth().Wei));

                host.Marketplace.MakeStorageAvailable(new StorageAvailability(
                    totalSpace: HostAvailabilitySize,
                    maxDuration: HostAvailabilityMaxDuration,
                    minPricePerBytePerSecond: 1.TstWei(),
                    totalCollateral: 999999.Tst())
                );
            }
            return hosts;
        }

        public TestToken GetTstBalance(ICodexNode node)
        {
            return GetContracts().GetTestTokenBalance(node);
        }

        public TestToken GetTstBalance(EthAddress address)
        {
            return GetContracts().GetTestTokenBalance(address);
        }

        public Ether GetEthBalance(ICodexNode node)
        {
            return GetGeth().GetEthBalance(node);
        }

        public Ether GetEthBalance(EthAddress address)
        {
            return GetGeth().GetEthBalance(address);
        }

        public ICodexNodeGroup StartClients()
        {
            return StartCodex(NumberOfClients, s => s
                .WithName("client")
                .EnableMarketplace(GetGeth(), GetContracts(), m => m
                    .WithInitial(StartingBalanceEth.Eth(), StartingBalanceTST.Tst())
                )
            );
        }

        public ICodexNode StartValidator()
        {
            return StartCodex(s => s
                .WithName("validator")
                .EnableMarketplace(GetGeth(), GetContracts(), m => m
                    .WithInitial(StartingBalanceEth.Eth(), StartingBalanceTST.Tst())
                    .AsValidator()
                )
            );
        }

        public SlotFill[] GetOnChainSlotFills(ICodexNodeGroup possibleHosts, string purchaseId)
        {
            var fills = GetOnChainSlotFills(possibleHosts);
            return fills.Where(f => f
                .SlotFilledEvent.RequestId.ToHex(false).ToLowerInvariant() == purchaseId.ToLowerInvariant())
                .ToArray();
        }

        public SlotFill[] GetOnChainSlotFills(ICodexNodeGroup possibleHosts)
        {
            var events = GetContracts().GetEvents(GetTestRunTimeRange());
            var fills = events.GetSlotFilledEvents();
            return fills.Select(f =>
            {
                var host = possibleHosts.Single(h => h.EthAddress.Address == f.Host.Address);
                return new SlotFill(f, host);

            }).ToArray();
        }

        protected void AssertClientHasPaidForContract(TestToken pricePerSlotPerSecond, ICodexNode client, IStoragePurchaseContract contract, ICodexNodeGroup hosts)
        {
            var balance = GetTstBalance(client);
            var expectedBalance = StartingBalanceTST.Tst() - GetContractFinalCost(pricePerSlotPerSecond, contract, hosts);

            Assert.That(balance, Is.EqualTo(expectedBalance), "Client balance incorrect.");
        }

        protected void AssertHostsWerePaidForContract(TestToken pricePerSlotPerSecond, IStoragePurchaseContract contract, ICodexNodeGroup hosts)
        {
            var fills = GetOnChainSlotFills(hosts);
            var submitUtc = GetContractOnChainSubmittedUtc(contract);
            var finishUtc = submitUtc + contract.Purchase.Duration;
            var expectedBalances = new Dictionary<EthAddress, TestToken>();
            foreach (var host in hosts) expectedBalances.Add(host.EthAddress, StartingBalanceTST.Tst());
            foreach (var fill in fills)
            {
                var slotDuration = finishUtc - fill.SlotFilledEvent.Block.Utc;
                expectedBalances[fill.Host.EthAddress] += GetContractCostPerSlot(pricePerSlotPerSecond, slotDuration);
            }

            foreach (var pair in expectedBalances)
            {
                var balance = GetTstBalance(pair.Key);
                Assert.That(balance, Is.EqualTo(pair.Value), "Host was not paid for storage.");
            }
        }

        protected void AssertHostsCollateralsAreUnchanged(ICodexNodeGroup hosts)
        {
            // There is no separate collateral location yet.
            // All host balances should be equal to or greater than the starting balance.
            foreach (var host in hosts)
            {
                Assert.That(GetTstBalance(host), Is.GreaterThanOrEqualTo(StartingBalanceTST.Tst()));
            }
        }

        private TestToken GetContractFinalCost(TestToken pricePerSlotPerSecond, IStoragePurchaseContract contract, ICodexNodeGroup hosts)
        {
            var fills = GetOnChainSlotFills(hosts);
            var result = 0.Tst();
            var submitUtc = GetContractOnChainSubmittedUtc(contract);
            var finishUtc = submitUtc + contract.Purchase.Duration;

            foreach (var fill in fills)
            {
                var slotDuration = finishUtc - fill.SlotFilledEvent.Block.Utc;
                result += GetContractCostPerSlot(pricePerSlotPerSecond, slotDuration);
            }

            return result;
        }

        private DateTime GetContractOnChainSubmittedUtc(IStoragePurchaseContract contract)
        {
            return Time.Retry<DateTime>(() =>
            {
                var events = GetContracts().GetEvents(GetTestRunTimeRange());
                var submitEvent = events.GetStorageRequests().SingleOrDefault(e => e.RequestId.ToHex(false) == contract.PurchaseId);
                if (submitEvent == null)
                {
                    // We're too early.
                    throw new TimeoutException(nameof(GetContractOnChainSubmittedUtc) + "StorageRequest not found on-chain.");
                }
                return submitEvent.Block.Utc;
            }, nameof(GetContractOnChainSubmittedUtc));
        }

        private TestToken GetContractCostPerSlot(TestToken pricePerSlotPerSecond, TimeSpan slotDuration)
        {
            return pricePerSlotPerSecond * (int)slotDuration.TotalSeconds;
        }

        protected void AssertContractSlotsAreFilledByHosts(IStoragePurchaseContract contract, ICodexNodeGroup hosts)
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

        protected void AssertContractIsOnChain(IStoragePurchaseContract contract)
        {
            AssertOnChainEvents(events =>
            {
                var onChainRequests = events.GetStorageRequests();
                if (onChainRequests.Any(r => r.Id == contract.PurchaseId)) return;
                throw new Exception($"OnChain request {contract.PurchaseId} not found...");
            }, nameof(AssertContractIsOnChain));
        }

        protected void AssertOnChainEvents(Action<ICodexContractsEvents> onEvents, string description)
        {
            Time.Retry(() =>
            {
                var events = GetContracts().GetEvents(GetTestRunTimeRange());
                onEvents(events);
            }, description);
        }

        public class SlotFill
        {
            public SlotFill(SlotFilledEventDTO slotFilledEvent, ICodexNode host)
            {
                SlotFilledEvent = slotFilledEvent;
                Host = host;
            }

            public SlotFilledEventDTO SlotFilledEvent { get; }
            public ICodexNode Host { get; }
        }

        private class MarketplaceHandle
        {
            public MarketplaceHandle(IGethNode geth, ICodexContracts contracts)
            {
                Geth = geth;
                Contracts = contracts;
            }

            public IGethNode Geth { get; }
            public ICodexContracts Contracts { get; }
        }
    }
}
