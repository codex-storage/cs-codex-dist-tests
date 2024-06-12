using CodexContractsPlugin;
using CodexNetDeployer;
using CodexPlugin;
using CodexTests.Helpers;
using Core;
using DistTestCore;
using DistTestCore.Helpers;
using DistTestCore.Logs;
using MetricsPlugin;
using Newtonsoft.Json;
using NUnit.Framework.Constraints;

namespace CodexTests
{
    public class CodexDistTest : DistTest
    {
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

        public ICodexNode StartCodex()
        {
            return StartCodex(s => { });
        }

        public ICodexNode StartCodex(Action<ICodexSetup> setup)
        {
            return StartCodex(1, setup)[0];
        }

        public ICodexNodeGroup StartCodex(int numberOfNodes)
        {
            return StartCodex(numberOfNodes, s => { });
        }

        public ICodexNodeGroup StartCodex(int numberOfNodes, Action<ICodexSetup> setup)
        {
            var group = Ci.StartCodexNodes(numberOfNodes, s =>
            {
                setup(s);
                OnCodexSetup(s);
            });

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
            Log($"Checking {node.GetName()} log for errors.");
            var log = Ci.DownloadLog(node);

            log.AssertLogDoesNotContain("Block validation failed");
            log.AssertLogDoesNotContain("ERR ");
        }

        public void LogNodeStatus(ICodexNode node, IMetricsAccess? metrics = null)
        {
            Log("Status for " + node.GetName() + Environment.NewLine +
                GetBasicNodeStatus(node));
        }

        private string GetBasicNodeStatus(ICodexNode node)
        {
            return JsonConvert.SerializeObject(node.GetDebugInfo(), Formatting.Indented) + Environment.NewLine +
                node.Space().ToString() + Environment.NewLine;
        }

        // Disabled for now: Makes huge log files!
        //private string GetNodeMetrics(IMetricsAccess? metrics)
        //{
        //    if (metrics == null) return "No metrics enabled";
        //    var m = metrics.GetAllMetrics();
        //    if (m == null) return "No metrics received";
        //    return m.AsCsv();
        //}

        protected virtual void OnCodexSetup(ICodexSetup setup)
        {
        }
    }
}
