using CodexClient;
using CodexContractsPlugin;
using CodexContractsPlugin.ChainMonitor;
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
            var monitor = new ChainMonitor(lifecycle.Log, contracts, lifecycle.TestStart, TimeSpan.FromSeconds(1.0));
            monitor.Start();

            handles.Add(lifecycle, new MarketplaceHandle(geth, contracts, monitor));
        }

        protected override void LifecycleStop(TestLifecycle lifecycle, DistTestResult result)
        {
            handles[lifecycle].Monitor.Stop();
            handles.Remove(lifecycle);
            base.LifecycleStop(lifecycle, result);
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
            return TimeSpan.FromSeconds(config.Proofs.Period);
        }

        protected PeriodMonitorResult GetPeriodMonitorReports()
        {
            return handles[Get()].Monitor.GetPeriodReports();
        }

        protected abstract int NumberOfHosts { get; }
        protected abstract int NumberOfClients { get; }
        protected abstract ByteSize HostAvailabilitySize { get; }
        protected abstract TimeSpan HostAvailabilityMaxDuration { get; }
        protected TimeSpan HostBlockTTL { get; } = TimeSpan.FromMinutes(1.0);

        public ICodexNodeGroup StartHosts()
        {
            var hosts = StartCodex(NumberOfHosts, s => s
                .WithName("host")
                .WithBlockTTL(HostBlockTTL)
                .WithBlockMaintenanceNumber(100)
                .WithBlockMaintenanceInterval(HostBlockTTL / 2)
                .EnableMarketplace(GetGeth(), GetContracts(), m => m
                    .WithInitial(StartingBalanceEth.Eth(), StartingBalanceTST.Tst())
                    .AsStorageNode()
                )
            );

            var config = GetContracts().Deployment.Config;
            foreach (var host in hosts)
            {
                AssertTstBalance(host, StartingBalanceTST.Tst(), nameof(StartHosts));
                AssertEthBalance(host, StartingBalanceEth.Eth(), nameof(StartHosts));

                var spaceBefore = host.Space();
                Assert.That(spaceBefore.QuotaReservedBytes, Is.EqualTo(0));
                Assert.That(spaceBefore.QuotaUsedBytes, Is.EqualTo(0));

                host.Marketplace.MakeStorageAvailable(new CreateStorageAvailability(
                    totalSpace: HostAvailabilitySize,
                    maxDuration: HostAvailabilityMaxDuration,
                    minPricePerBytePerSecond: 1.TstWei(),
                    totalCollateral: 999999.Tst())
                );

                var spaceAfter = host.Space();
                Assert.That(spaceAfter.QuotaReservedBytes, Is.EqualTo(HostAvailabilitySize.SizeInBytes));
                Assert.That(spaceAfter.QuotaUsedBytes, Is.EqualTo(0));
            }
            return hosts;
        }

        public void AssertHostAvailabilitiesAreEmpty(IEnumerable<ICodexNode> hosts)
        {
            var retry = GetAvailabilitySpaceAssertRetry();
            retry.Run(() =>
            {
                foreach (var host in hosts)
                {
                    AssertHostAvailabilitiesAreEmpty(host);
                }
            });
        }

        private void AssertHostAvailabilitiesAreEmpty(ICodexNode host)
        {
            var availabilities = host.Marketplace.GetAvailabilities();
            foreach (var a in availabilities)
            {
                if (a.FreeSpace.SizeInBytes  != a.TotalSpace.SizeInBytes)
                {
                    throw new Exception(nameof(AssertHostAvailabilitiesAreEmpty) + $" free: {a.FreeSpace} total: {a.TotalSpace}");
                }
            }
        }

        public void AssertTstBalance(ICodexNode node, TestToken expectedBalance, string message)
        {
            AssertTstBalance(node.EthAddress, expectedBalance, message);
        }

        public void AssertTstBalance(EthAddress address, TestToken expectedBalance, string message)
        {
            var retry = GetBalanceAssertRetry();
            retry.Run(() =>
            {
                var balance = GetTstBalance(address);

                if (balance != expectedBalance)
                {
                    throw new Exception(nameof(AssertTstBalance) +
                        $" expected: {expectedBalance} but was: {balance} - message: " + message);
                }
            });
        }

        public void AssertEthBalance(ICodexNode node, Ether expectedBalance, string message)
        {
            var retry = GetBalanceAssertRetry();
            retry.Run(() =>
            {
                var balance = GetEthBalance(node);

                if (balance != expectedBalance)
                {
                    throw new Exception(nameof(AssertEthBalance) + 
                        $" expected: {expectedBalance} but was: {balance} - message: " + message);
                }
            });
        }

        private Retry GetBalanceAssertRetry()
        {
            return new Retry("AssertBalance",
                maxTimeout: TimeSpan.FromMinutes(10.0),
                sleepAfterFail: TimeSpan.FromSeconds(10.0),
                onFail: f => { },
                failFast: false);
        }

        private Retry GetAvailabilitySpaceAssertRetry()
        {
            return new Retry("AssertAvailabilitySpace",
                maxTimeout: HostBlockTTL * 3,
                sleepAfterFail: TimeSpan.FromSeconds(10.0),
                onFail: f => { },
                failFast: false);
        }

        private TestToken GetTstBalance(ICodexNode node)
        {
            return GetContracts().GetTestTokenBalance(node);
        }

        private TestToken GetTstBalance(EthAddress address)
        {
            return GetContracts().GetTestTokenBalance(address);
        }

        private Ether GetEthBalance(ICodexNode node)
        {
            return GetGeth().GetEthBalance(node);
        }

        private Ether GetEthBalance(EthAddress address)
        {
            return GetGeth().GetEthBalance(address);
        }

        public ICodexNodeGroup StartClients()
        {
            return StartClients(s => { });
        }

        public ICodexNodeGroup StartClients(Action<ICodexSetup> additional)
        {
            return StartCodex(NumberOfClients, s =>
            {
                s.WithName("client")
                    .EnableMarketplace(GetGeth(), GetContracts(), m => m
                    .WithInitial(StartingBalanceEth.Eth(), StartingBalanceTST.Tst()));

                additional(s);
            });
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

        public SlotFree[] GetOnChainSlotFrees(ICodexNodeGroup possibleHosts, string purchaseId)
        {
            var fills = GetOnChainSlotFrees(possibleHosts);
            return fills.Where(f => f
                .SlotFreedEvent.RequestId.ToHex(false).ToLowerInvariant() == purchaseId.ToLowerInvariant())
                .ToArray();
        }

        public SlotFree[] GetOnChainSlotFrees(ICodexNodeGroup possibleHosts)
        {
            var events = GetContracts().GetEvents(GetTestRunTimeRange());
            var fills = GetOnChainSlotFills(possibleHosts);
            var frees = events.GetSlotFreedEvents();
            return frees.Select(f =>
            {
                var matchingFill = fills.Single(fill => fill.SlotFilledEvent.RequestId == f.RequestId &&
                    fill.SlotFilledEvent.SlotIndex == f.SlotIndex);

                return new SlotFree(f, matchingFill.Host);

            }).ToArray();
        }

        protected void AssertClientHasPaidForContract(TestToken pricePerBytePerSecond, ICodexNode client, IStoragePurchaseContract contract, ICodexNodeGroup hosts)
        {
            var expectedBalance = StartingBalanceTST.Tst() - GetContractFinalCost(pricePerBytePerSecond, contract, hosts);

            AssertTstBalance(client, expectedBalance, "Client balance incorrect.");
        }

        protected void AssertHostsWerePaidForContract(TestToken pricePerBytePerSecond, IStoragePurchaseContract contract, ICodexNodeGroup hosts)
        {
            var fills = GetOnChainSlotFills(hosts);
            var submitUtc = GetContractOnChainSubmittedUtc(contract);
            var finishUtc = submitUtc + contract.Purchase.Duration;
            var slotSize = Convert.ToInt64(contract.GetStatus().Request.Ask.SlotSize).Bytes();
            var expectedBalances = new Dictionary<EthAddress, TestToken>();

            foreach (var host in hosts) expectedBalances.Add(host.EthAddress, StartingBalanceTST.Tst());
            foreach (var fill in fills)
            {
                var slotDuration = finishUtc - fill.SlotFilledEvent.Block.Utc;
                expectedBalances[fill.Host.EthAddress] += GetContractCostPerSlot(pricePerBytePerSecond, slotSize, slotDuration);
            }

            foreach (var pair in expectedBalances)
            {
                AssertTstBalance(pair.Key, pair.Value, "Host was not paid for storage.");
            }
        }

        protected void AssertHostsCollateralsAreUnchanged(ICodexNodeGroup hosts)
        {
            // There is no separate collateral location yet.
            // All host balances should be equal to or greater than the starting balance.
            foreach (var host in hosts)
            {
                var retry = GetBalanceAssertRetry();
                retry.Run(() =>
                {
                    if (GetTstBalance(host) < StartingBalanceTST.Tst())
                    {
                        throw new Exception(nameof(AssertHostsCollateralsAreUnchanged));
                    }
                });
            }
        }

        private TestToken GetContractFinalCost(TestToken pricePerBytePerSecond, IStoragePurchaseContract contract, ICodexNodeGroup hosts)
        {
            var fills = GetOnChainSlotFills(hosts);
            var result = 0.Tst();
            var submitUtc = GetContractOnChainSubmittedUtc(contract);
            var finishUtc = submitUtc + contract.Purchase.Duration;
            var slotSize = Convert.ToInt64(contract.GetStatus().Request.Ask.SlotSize).Bytes();

            foreach (var fill in fills)
            {
                var slotDuration = finishUtc - fill.SlotFilledEvent.Block.Utc;
                result += GetContractCostPerSlot(pricePerBytePerSecond, slotSize, slotDuration);
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

        private TestToken GetContractCostPerSlot(TestToken pricePerBytePerSecond, ByteSize slotSize, TimeSpan slotDuration)
        {
            var cost = pricePerBytePerSecond.TstWei * slotSize.SizeInBytes * (int)slotDuration.TotalSeconds;
            return cost.TstWei();
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

        public class SlotFree
        {
            public SlotFree(SlotFreedEventDTO slotFreedEvent, ICodexNode host)
            {
                SlotFreedEvent = slotFreedEvent;
                Host = host;
            }

            public SlotFreedEventDTO SlotFreedEvent { get; }
            public ICodexNode Host { get; }
        }

        private class MarketplaceHandle
        {
            public MarketplaceHandle(IGethNode geth, ICodexContracts contracts, ChainMonitor monitor)
            {
                Geth = geth;
                Contracts = contracts;
                Monitor = monitor;
            }

            public IGethNode Geth { get; }
            public ICodexContracts Contracts { get; }
            public ChainMonitor Monitor { get; }
        }
    }
}
