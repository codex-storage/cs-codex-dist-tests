using BlockchainUtils;
using CodexClient;
using CodexContractsPlugin;
using CodexNetDeployer;
using CodexPlugin;
using CodexPlugin.OverwatchSupport;
using CodexTests.Helpers;
using Core;
using DistTestCore;
using DistTestCore.Helpers;
using DistTestCore.Logs;
using GethPlugin;
using Logging;
using MetricsPlugin;
using Newtonsoft.Json;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using OverwatchTranscript;
using Utils;

namespace CodexTests
{
    public class CodexDistTest : DistTest
    {
        private readonly BlockCache blockCache = new BlockCache();
        private readonly List<ICodexNode> nodes = new List<ICodexNode>();
        private CodexTranscriptWriter? writer;

        public CodexDistTest()
        {
            ProjectPlugin.Load<CodexPlugin.CodexPlugin>();
            ProjectPlugin.Load<CodexContractsPlugin.CodexContractsPlugin>();
            ProjectPlugin.Load<GethPlugin.GethPlugin>();
            ProjectPlugin.Load<MetricsPlugin.MetricsPlugin>();
        }

        [SetUp]
        public void SetupCodexDistTest()
        {
            writer = SetupTranscript();
        }

        [TearDown]
        public void TearDownCodexDistTest()
        {
            TeardownTranscript();
        }

        protected override void Initialize(FixtureLog fixtureLog)
        {
            var localBuilder = new LocalCodexBuilder(fixtureLog);
            localBuilder.Intialize();
            localBuilder.Build();

            Ci.AddCodexHooksProvider(new CodexLogTrackerProvider(nodes.Add));
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

        public IGethNode StartGethNode(Action<IGethSetup> setup)
        {
            return Ci.StartGethNode(blockCache, setup);
        }

        public PeerConnectionTestHelpers CreatePeerConnectionTestHelpers()
        {
            return new PeerConnectionTestHelpers(GetTestLog());
        }

        public PeerDownloadTestHelpers CreatePeerDownloadTestHelpers()
        {
            return new PeerDownloadTestHelpers(GetTestLog(), GetFileManager());
        }

        public void AssertBalance(ICodexContracts contracts, ICodexNode codexNode, Constraint constraint, string msg)
        {
            Assert.Fail("Depricated, use MarketplaceAutobootstrapDistTest assertBalances instead.");
            AssertHelpers.RetryAssert(constraint, () => contracts.GetTestTokenBalance(codexNode), nameof(AssertBalance) + msg);
        }

        public void CheckLogForErrors(params ICodexNode[] nodes)
        {
            foreach (var node in nodes) CheckLogForErrors(node);
        }

        public void CheckLogForErrors(ICodexNode node)
        {
            Log($"Checking {node.GetName()} log for errors.");
            var log = node.DownloadLog();

            log.AssertLogDoesNotContain("Block validation failed");
            log.AssertLogDoesNotContainLinesStartingWith("ERR ");
        }

        public void LogNodeStatus(ICodexNode node, IMetricsAccess? metrics = null)
        {
            Log("Status for " + node.GetName() + Environment.NewLine +
                GetBasicNodeStatus(node));
        }

        public void WaitAndCheckNodesStaysAlive(TimeSpan duration, ICodexNodeGroup nodes)
        {
            WaitAndCheckNodesStaysAlive(duration, nodes.ToArray());
        }

        public void WaitAndCheckNodesStaysAlive(TimeSpan duration, List<ICodexNode> nodes)
        {
            WaitAndCheckNodesStaysAlive(duration, nodes.ToArray());
        }

        public void WaitAndCheckNodesStaysAlive(TimeSpan duration, params ICodexNode[] nodes)
        {
            Log($"{nameof(WaitAndCheckNodesStaysAlive)} {Time.FormatDuration(duration)}...");

            var timeout = TimeSpan.FromSeconds(3.0);
            Assert.That(duration.TotalSeconds, Is.GreaterThan(timeout.TotalSeconds));

            var start = DateTime.UtcNow;
            while ((DateTime.UtcNow - start) < duration)
            {
                Thread.Sleep(timeout);
                foreach (var node in nodes)
                {
                    Assert.That(node.HasCrashed(), Is.False);

                    var info = node.GetDebugInfo();
                    Assert.That(!string.IsNullOrEmpty(info.Id));
                }
            }

            Log($"{nameof(WaitAndCheckNodesStaysAlive)} OK");
        }

        public void AssertNodesContainFile(ContentId cid, ICodexNodeGroup nodes)
        {
            AssertNodesContainFile(cid, nodes.ToArray());
        }

        public void AssertNodesContainFile(ContentId cid, params ICodexNode[] nodes)
        {
            Log($"{nameof(AssertNodesContainFile)} {nodes.Names()} {cid}...");

            foreach (var node in nodes)
            {
                var localDatasets = node.LocalFiles();
                CollectionAssert.Contains(localDatasets.Content.Select(c => c.Cid), cid);
            }

            Log($"{nameof(AssertNodesContainFile)} OK");
        }

        private string GetBasicNodeStatus(ICodexNode node)
        {
            return JsonConvert.SerializeObject(node.GetDebugInfo(), Formatting.Indented) + Environment.NewLine +
                node.Space().ToString() + Environment.NewLine;
        }

        protected virtual void OnCodexSetup(ICodexSetup setup)
        {
        }

        private CreateTranscriptAttribute? GetTranscriptAttributeOfCurrentTest()
        {
            var attrs = GetCurrentTestMethodAttribute<CreateTranscriptAttribute>();
            if (attrs.Any()) return attrs.Single();
            return null;
        }

        private CodexTranscriptWriter? SetupTranscript()
        {
            var attr = GetTranscriptAttributeOfCurrentTest();
            if (attr == null) return null;

            var config = new CodexTranscriptWriterConfig(
                attr.OutputFilename,
                attr.IncludeBlockReceivedEvents
            );

            var log = new LogPrefixer(GetTestLog(), "(Transcript) ");
            var writer = new CodexTranscriptWriter(log, config, Transcript.NewWriter(log));
            Ci.AddCodexHooksProvider(writer);
            return writer;
        }

        private void TeardownTranscript()
        {
            if (writer == null) return;

            var result = GetTestResult();
            var log = GetTestLog();
            writer.AddResult(result.Success, result.Result);
            try
            {
                Stopwatch.Measure(log, "Transcript.ProcessLogs", () =>
                {
                    writer.ProcessLogs(DownloadAllLogs());
                });

                Stopwatch.Measure(log, $"Transcript.FinalizeWriter", () =>
                {
                    writer.IncludeFile(log.GetFullName() + ".log");
                    writer.FinalizeWriter();
                });
            }
            catch (Exception ex)
            {
                log.Error("Failure during transcript teardown: " + ex);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CreateTranscriptAttribute : PropertyAttribute
    {
        public CreateTranscriptAttribute(string outputFilename, bool includeBlockReceivedEvents = true)
        {
            OutputFilename = outputFilename;
            IncludeBlockReceivedEvents = includeBlockReceivedEvents;
        }

        public string OutputFilename { get; }
        public bool IncludeBlockReceivedEvents { get; }
    }
}
