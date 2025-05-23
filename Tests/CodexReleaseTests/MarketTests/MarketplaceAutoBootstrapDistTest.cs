﻿using CodexClient;
using CodexContractsPlugin;
using CodexContractsPlugin.Marketplace;
using CodexPlugin;
using CodexTests;
using GethPlugin;
using Logging;
using Nethereum.Hex.HexConvertors.Extensions;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.MarketTests
{
    public abstract class MarketplaceAutoBootstrapDistTest : AutoBootstrapDistTest
    {
        private MarketplaceHandle handle = null!;
        protected const int StartingBalanceTST = 1000;
        protected const int StartingBalanceEth = 10;

        [SetUp]
        public void SetupMarketplace()
        {
            var geth = StartGethNode(s => s.IsMiner());
            var contracts = Ci.StartCodexContracts(geth, BootstrapNode.Version);
            var monitor = SetupChainMonitor(GetTestLog(), contracts, GetTestRunTimeRange().From);
            handle = new MarketplaceHandle(geth, contracts, monitor);
        }

        [TearDown]
        public void TearDownMarketplace()
        {
            if (handle.ChainMonitor != null) handle.ChainMonitor.Stop();
        }

        protected IGethNode GetGeth()
        {
            return handle.Geth;
        }

        protected ICodexContracts GetContracts()
        {
            return handle.Contracts;
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
        protected virtual bool MonitorChainState { get; } = true;

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
                AssertTstBalance(host, StartingBalanceTST.Tst(), nameof(StartHosts));
                AssertEthBalance(host, StartingBalanceEth.Eth(), nameof(StartHosts));
                
                host.Marketplace.MakeStorageAvailable(new StorageAvailability(
                    totalSpace: HostAvailabilitySize,
                    maxDuration: HostAvailabilityMaxDuration,
                    minPricePerBytePerSecond: 1.TstWei(),
                    totalCollateral: 999999.Tst())
                );
            }
            return hosts;
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

        private ChainMonitor? SetupChainMonitor(ILog log, ICodexContracts contracts, DateTime startUtc)
        {
            if (!MonitorChainState) return null;

            var result = new ChainMonitor(log, contracts, startUtc);
            result.Start(() =>
            {
                log.Error("Failure in chain monitor. No chain updates after this point.");
                //Assert.Fail("Failure in chain monitor.");
            });
            return result;
        }

        private Retry GetBalanceAssertRetry()
        {
            return new Retry("AssertBalance",
                maxTimeout: TimeSpan.FromMinutes(10.0),
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

        protected void AssertClientHasPaidForContract(TestToken pricePerBytePerSecond, ICodexNode client, IStoragePurchaseContract contract, ICodexNodeGroup hosts)
        {
            var expectedBalance = StartingBalanceTST.Tst() - GetContractFinalCost(pricePerBytePerSecond, contract, hosts);

            AssertTstBalance(client, expectedBalance, "Client balance incorrect.");

            Log($"Client has paid for contract. Balance: {expectedBalance}");
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
                AssertTstBalance(pair.Key, pair.Value, $"Host {pair.Key} was not paid for storage.");

                Log($"Host {pair.Key} was paid for storage. Balance: {pair.Value}");
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

        protected void WaitForContractStarted(IStoragePurchaseContract r)
        {
            try
            {
                r.WaitForStorageContractStarted();
            }
            catch
            {
                // Contract failed to start. Retrieve and log every call to ReserveSlot to identify which hosts
                // should have filled the slot.

                var requestId = r.PurchaseId.ToLowerInvariant();
                var calls = new List<ReserveSlotFunction>();
                GetContracts().GetEvents(GetTestRunTimeRange()).GetReserveSlotCalls(calls.Add);

                Log($"Request '{requestId}' failed to start. There were {calls.Count} hosts who called reserve-slot for it:");
                foreach (var c in calls)
                {
                    Log($" - {c.Block.Utc} Host: {c.FromAddress} RequestId: {c.RequestId.ToHex()} SlotIndex: {c.SlotIndex}");
                }
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

        private class MarketplaceHandle
        {
            public MarketplaceHandle(IGethNode geth, ICodexContracts contracts, ChainMonitor? chainMonitor)
            {
                Geth = geth;
                Contracts = contracts;
                ChainMonitor = chainMonitor;
            }

            public IGethNode Geth { get; }
            public ICodexContracts Contracts { get; }
            public ChainMonitor? ChainMonitor { get; }
        }
    }
}
