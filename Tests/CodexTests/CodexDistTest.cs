using CodexContractsPlugin;
using CodexNetDeployer;
using CodexPlugin;
using CodexTests.Helpers;
using Core;
using DistTestCore;
using DistTestCore.Helpers;
using DistTestCore.Logs;
using NUnit.Framework.Constraints;

namespace CodexTests
{
    public class  CodexDistTest : DistTest
    {
        private readonly Dictionary<TestLifecycle, List<ICodexNode>> onlineCodexNodes = new Dictionary<TestLifecycle, List<ICodexNode>>();

        public CodexDistTest()
        {
            ProjectPlugin.Load<CodexPlugin.CodexPlugin>();
            ProjectPlugin.Load<CodexContractsPlugin.CodexContractsPlugin>();
            ProjectPlugin.Load<GethPlugin.GethPlugin>();
            ProjectPlugin.Load<MetricsPlugin.MetricsPlugin>();
        }

        protected override void Initialize(FixtureLog fixtureLog)
        {
            var localBuilder = new LocalCodexBuilder(fixtureLog);
            localBuilder.Intialize();
            localBuilder.Build();
        }

        protected override void LifecycleStart(TestLifecycle lifecycle)
        {
            onlineCodexNodes.Add(lifecycle, new List<ICodexNode>());
        }

        protected override void LifecycleStop(TestLifecycle lifecycle)
        {
            onlineCodexNodes.Remove(lifecycle);
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
            onlineCodexNodes[Get()].AddRange(group);
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
            return onlineCodexNodes[Get()];
        }

        public void AssertBalance(ICodexContracts contracts, ICodexNode codexNode, Constraint constraint, string msg = "")
        {
            AssertHelpers.RetryAssert(constraint, () => contracts.GetTestTokenBalance(codexNode), nameof(AssertBalance) + msg);
        }

        public void CheckLogForErrors(params ICodexNode[] nodes)
        {
            foreach (var node in nodes) CheckLogForErrors(node);
        }

        public void CheckLogForErrors(ICodexNode node)
        {
            var log = Ci.DownloadLog(node);

            log.AssertLogDoesNotContain("Block validation failed");
            log.AssertLogDoesNotContain("ERR ");
        }

        protected virtual void OnCodexSetup(ICodexSetup setup)
        {
        }

        protected override void CollectStatusLogData(TestLifecycle lifecycle, Dictionary<string, string> data)
        {
            var nodes = onlineCodexNodes[lifecycle];
            var upload = nodes.Select(n => n.TransferSpeeds.GetUploadSpeed()).ToList()!.OptionalAverage();
            var download = nodes.Select(n => n.TransferSpeeds.GetDownloadSpeed()).ToList()!.OptionalAverage();
            if (upload != null) data.Add("avgupload", upload.ToString());
            if (download != null) data.Add("avgdownload", download.ToString());
        }
    }
}
