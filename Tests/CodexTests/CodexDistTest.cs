using CodexContractsPlugin;
using CodexNetDeployer;
using CodexPlugin;
using Core;
using DistTestCore;
using DistTestCore.Helpers;
using DistTestCore.Logs;
using GethPlugin;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace CodexTests
{
    public class CodexDistTest : DistTest
    {
        private readonly List<ICodexNode> onlineCodexNodes = new List<ICodexNode>();

        public CodexDistTest()
        {
            ProjectPlugin.Load<CodexPlugin.CodexPlugin>();
            ProjectPlugin.Load<CodexContractsPlugin.CodexContractsPlugin>();
            ProjectPlugin.Load<GethPlugin.GethPlugin>();
            ProjectPlugin.Load<MetricsPlugin.MetricsPlugin>();
        }

        [TearDown]
        public void TearDownCodexFixture()
        {
            onlineCodexNodes.Clear();
        }

        protected override void Initialize(FixtureLog fixtureLog)
        {
            var localBuilder = new LocalCodexBuilder(fixtureLog);
            localBuilder.Intialize();
            localBuilder.Build();
        }

        public ICodexNode AddCodex()
        {
            return AddCodex(s => { });
        }

        public ICodexNode AddCodex(Action<ICodexSetup> setup)
        {
            return AddCodex(1, setup)[0];
        }

        public ICodexNodeGroup AddCodex(int numberOfNodes)
        {
            return AddCodex(numberOfNodes, s => { });
        }

        public ICodexNodeGroup AddCodex(int numberOfNodes, Action<ICodexSetup> setup)
        {
            var group = Ci.StartCodexNodes(numberOfNodes, s =>
            {
                setup(s);
                OnCodexSetup(s);
            });
            onlineCodexNodes.AddRange(group);
            return group;
        }

        public PeerConnectionTestHelpers CreatePeerConnectionTestHelpers()
        {
            return new PeerConnectionTestHelpers(GetTestLog());
        }

        public PeerDownloadTestHelpers CreatePeerDownloadTestHelpers()
        {
            return new PeerDownloadTestHelpers(GetTestLog(), Get().GetFileManager());
        }

        public IEnumerable<ICodexNode> GetAllOnlineCodexNodes()
        {
            return onlineCodexNodes;
        }

        public void AssertBalance(ICodexContracts contracts, ICodexNode codexNode, Constraint constraint, string msg = "")
        {
            AssertHelpers.RetryAssert(constraint, () => contracts.GetTestTokenBalance(codexNode), nameof(AssertBalance) + msg);
        }

        protected virtual void OnCodexSetup(ICodexSetup setup)
        {
        }
    }
}
