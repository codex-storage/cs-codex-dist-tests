using CodexContractsPlugin;
using CodexContractsPlugin.Marketplace;
using CodexPlugin;
using CodexTests;
using DistTestCore;
using GethPlugin;
using Nethereum.Hex.HexConvertors.Extensions;
using Utils;

namespace CodexReleaseTests.MarketTests
{
    public abstract class MarketplaceAutoBootstrapDistTest : AutoBootstrapDistTest
    {
        private readonly Dictionary<TestLifecycle, MarketplaceHandle> handles = new Dictionary<TestLifecycle, MarketplaceHandle>();
        protected const int StartingBalanceTST = 1000;

        protected override void LifecycleStart(TestLifecycle lifecycle)
        {
            base.LifecycleStart(lifecycle);
            var geth = Ci.StartGethNode(s => s.IsMiner());
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

        protected abstract int NumberOfHosts { get; }
        protected abstract int NumberOfClients { get; }
        protected abstract ByteSize HostAvailabilitySize { get; }
        protected abstract TimeSpan HostAvailabilityMaxDuration { get; }

        public ICodexNodeGroup StartHosts()
        {
            var hosts = StartCodex(NumberOfHosts, s => s
                .WithName("host")
                .EnableMarketplace(GetGeth(), GetContracts(), m => m
                    .WithInitial(10.Eth(), StartingBalanceTST.Tst())
                    .AsStorageNode()
                )
            );

            var config = GetContracts().Deployment.Config;
            foreach (var host in hosts)
            {
                host.Marketplace.MakeStorageAvailable(new CodexPlugin.StorageAvailability(
                    totalSpace: HostAvailabilitySize,
                    maxDuration: HostAvailabilityMaxDuration,
                    minPriceForTotalSpace: 1.TstWei(),
                    maxCollateral: 999999.Tst())
                );
            }
            return hosts;
        }

        public ICodexNodeGroup StartClients()
        {
            return StartCodex(NumberOfClients, s => s
                .WithName("client")
                .EnableMarketplace(GetGeth(), GetContracts(), m => m
                    .WithInitial(10.Eth(), StartingBalanceTST.Tst())
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
