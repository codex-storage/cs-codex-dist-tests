using CodexClient;
using CodexContractsPlugin;
using CodexContractsPlugin.Marketplace;
using CodexPlugin;
using CodexTests;
using GethPlugin;
using Logging;
using Nethereum.Hex.HexConvertors.Extensions;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.Utils
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
            var monitor = SetupChainMonitor(GetTestLog(), geth, contracts, GetTestRunTimeRange().From);
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
            return TimeSpan.FromSeconds(config.Proofs.Period);
        }

        protected abstract int NumberOfHosts { get; }
        protected abstract int NumberOfClients { get; }
        protected abstract ByteSize HostAvailabilitySize { get; }
        protected abstract TimeSpan HostAvailabilityMaxDuration { get; }
        protected virtual bool MonitorChainState { get; } = true;
        protected TimeSpan HostBlockTTL { get; } = TimeSpan.FromMinutes(1.0);

        public ICodexNodeGroup StartHosts()
        {
            var hosts = StartCodex(NumberOfHosts, s => s
                .WithName("host")
                .WithBlockTTL(HostBlockTTL)
                .WithBlockMaintenanceNumber(1000)
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
                
                host.Marketplace.MakeStorageAvailable(new CreateStorageAvailability(
                    totalSpace: HostAvailabilitySize,
                    maxDuration: HostAvailabilityMaxDuration,
                    minPricePerBytePerSecond: 1.TstWei(),
                    totalCollateral: 999999.Tst())
                );
            }
            return hosts;
        }

        public ICodexNode StartOneHost()
        {
            var host = StartCodex(s => s
                .WithName("singlehost")
                .WithBlockTTL(HostBlockTTL)
                .WithBlockMaintenanceNumber(1000)
                .WithBlockMaintenanceInterval(HostBlockTTL / 2)
                .EnableMarketplace(GetGeth(), GetContracts(), m => m
                    .WithInitial(StartingBalanceEth.Eth(), StartingBalanceTST.Tst())
                    .AsStorageNode()
                )
            );

            var config = GetContracts().Deployment.Config;
            AssertTstBalance(host, StartingBalanceTST.Tst(), nameof(StartOneHost));
            AssertEthBalance(host, StartingBalanceEth.Eth(), nameof(StartOneHost));

            host.Marketplace.MakeStorageAvailable(new CreateStorageAvailability(
                totalSpace: HostAvailabilitySize,
                maxDuration: HostAvailabilityMaxDuration,
                minPricePerBytePerSecond: 1.TstWei(),
                totalCollateral: 999999.Tst())
            );
            return host;
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
                if (a.FreeSpace.SizeInBytes != a.TotalSpace.SizeInBytes)
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

        private ChainMonitor? SetupChainMonitor(ILog log, IGethNode gethNode, ICodexContracts contracts, DateTime startUtc)
        {
            if (!MonitorChainState) return null;

            var result = new ChainMonitor(log, gethNode, contracts, startUtc);
            result.Start(() =>
            {
                Assert.Fail("Failure in chain monitor.");
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

        public SlotFill[] GetOnChainSlotFills(IEnumerable<ICodexNode> possibleHosts, string purchaseId)
        {
            var fills = GetOnChainSlotFills(possibleHosts);
            return fills.Where(f => f
                .SlotFilledEvent.RequestId.ToHex(false).ToLowerInvariant() == purchaseId.ToLowerInvariant())
                .ToArray();
        }

        public SlotFill[] GetOnChainSlotFills(IEnumerable<ICodexNode> possibleHosts)
        {
            var events = GetContracts().GetEvents(GetTestRunTimeRange());
            var fills = events.GetSlotFilledEvents();
            return fills.Select(f =>
            {
                // We can encounter a fill event that's from an old host.
                // We must disregard those.
                var host = possibleHosts.SingleOrDefault(h => h.EthAddress.Address == f.Host.Address);
                if (host == null) return null;
                return new SlotFill(f, host);
            })
            .Where(f => f != null)
            .Cast<SlotFill>()
            .ToArray();
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
                throw;
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
            return Time.Retry(() =>
            {
                var events = GetContracts().GetEvents(GetTestRunTimeRange());
                var submitEvent = events.GetStorageRequestedEvents().SingleOrDefault(e => e.RequestId.ToHex() == contract.PurchaseId);
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
            // Check the creation event.
            AssertOnChainEvents(events =>
            {
                var onChainRequests = events.GetStorageRequestedEvents();
                if (onChainRequests.Any(r => r.RequestId.ToHex() == contract.PurchaseId)) return;
                throw new Exception($"OnChain request {contract.PurchaseId} not found...");
            }, nameof(AssertContractIsOnChain));

            // Check that the getRequest call returns it.
            var rid = contract.PurchaseId.HexToByteArray();
            var r = GetContracts().GetRequest(rid);
            if (r == null) throw new Exception($"Failed to get Request from {nameof(GetRequestFunction)}");
            Assert.That(r.Ask.Duration, Is.EqualTo(contract.Purchase.Duration.TotalSeconds));
            Assert.That(r.Ask.Slots, Is.EqualTo(contract.Purchase.MinRequiredNumberOfNodes));
            Assert.That(((int)r.Ask.ProofProbability), Is.EqualTo(contract.Purchase.ProofProbability));
        }

        protected void AssertOnChainEvents(Action<ICodexContractsEvents> onEvents, string description)
        {
            Time.Retry(() =>
            {
                var events = GetContracts().GetEvents(GetTestRunTimeRange());
                onEvents(events);
            }, description);
        }

        protected TimeSpan CalculateContractFailTimespan()
        {
            var config = GetContracts().Deployment.Config;
            var requiredNumMissedProofs = Convert.ToInt32(config.Collateral.MaxNumberOfSlashes);
            var periodDuration = GetPeriodDuration();
            var gracePeriod = periodDuration;

            // Each host could miss 1 proof per period,
            // so the time we should wait is period time * requiredNum of missed proofs.
            // Except: the proof requirement has a concept of "downtime":
            // a segment of time where proof is not required.
            // We calculate the probability of downtime and extend the waiting
            // timeframe by a factor, such that all hosts are highly likely to have 
            // failed a sufficient number of proofs.

            float n = requiredNumMissedProofs;
            return gracePeriod + periodDuration * n * GetDowntimeFactor(config);
        }

        private float GetDowntimeFactor(MarketplaceConfig config)
        {
            byte numBlocksInDowntimeSegment = config.Proofs.Downtime;
            float downtime = numBlocksInDowntimeSegment;
            float window = 256.0f;
            var chanceOfDowntime = downtime / window;
            return 1.0f + (5.0f * chanceOfDowntime);
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
