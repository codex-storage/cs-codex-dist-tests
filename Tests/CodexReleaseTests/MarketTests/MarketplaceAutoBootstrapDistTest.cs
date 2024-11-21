using CodexContractsPlugin;
using CodexPlugin;
using CodexTests;
using DistTestCore;
using GethPlugin;

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

        protected override void OnCodexSetup(ICodexSetup setup)
        {
            base.OnCodexSetup(setup);
            setup.EnableMarketplace(GetGeth(), GetContracts(), m => m.WithInitial(10.Eth(), StartingBalanceTST.Tst()));
        }

        protected IGethNode GetGeth()
        {
            return handles[Get()].Geth;
        }

        protected ICodexContracts GetContracts()
        {
            return handles[Get()].Contracts;
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
