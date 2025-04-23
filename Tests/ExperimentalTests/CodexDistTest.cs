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

namespace CodexTests
{
    public class CodexDistTestComponents : ILifecycleComponent
    {
        private readonly object nodesLock = new object();

        public CodexDistTestComponents(CodexTranscriptWriter? writer)
        {
            Writer = writer;
        }

        public CodexTranscriptWriter? Writer { get; }
        public BlockCache Cache { get; } = new();
        public List<ICodexNode> Nodes { get; } = new();

        public void Start(ILifecycleComponentAccess access)
        {
            var ci = access.Get<TestLifecycle>().CoreInterface;
            ci.AddCodexHooksProvider(new CodexLogTrackerProvider(n =>
            {
                lock (nodesLock)
                {
                    Nodes.Add(n);
                }
            }));
        }

        public void Stop(ILifecycleComponentAccess access, DistTestResult result)
        {
            var tl = access.Get<TestLifecycle>();
            var log = tl.Log;
            var logFiles = tl.DownloadAllLogs();

            TeardownTranscript(log, logFiles, result);

            // todo: on not success: go to nodes and dl logs?
            // or fix disttest failure log download so we can always have logs even for non-codexes?
        }

        private void TeardownTranscript(TestLog log, IDownloadedLog[] logFiles, DistTestResult result)
        {
            if (Writer == null) return;

            Writer.AddResult(result.Success, result.Result);

            try
            {
                Stopwatch.Measure(log, "Transcript.ProcessLogs", () =>
                {
                    Writer.ProcessLogs(logFiles);
                });

                Stopwatch.Measure(log, $"Transcript.Finalize", () =>
                {
                    Writer.IncludeFile(log.GetFullName());
                    Writer.Finalize();
                });
            }
            catch (Exception ex)
            {
                log.Error("Failure during transcript teardown: " + ex);
            }
        }
    }

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

        protected override void CreateComponents(ILifecycleComponentCollector collector)
        {
            base.CreateComponents(collector);
            collector.AddComponent(new CodexDistTestComponents(
                SetupTranscript()
            ));
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
            return Ci.StartGethNode(GetBlockCache(), setup);
        }

        private BlockCache GetBlockCache()
        {
            return Get<CodexDistTestComponents>().Cache;
        }

        public PeerConnectionTestHelpers CreatePeerConnectionTestHelpers()
        {
            return new PeerConnectionTestHelpers(GetTestLog());
        }

        public PeerDownloadTestHelpers CreatePeerDownloadTestHelpers()
        {
            return new PeerDownloadTestHelpers(GetTestLog(), Get<TestLifecycle>().GetFileManager());
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

        public void WaitAndCheckNodesStaysAlive(TimeSpan duration, params ICodexNode[] nodes)
        {
            var start = DateTime.UtcNow;
            while ((DateTime.UtcNow - start) < duration)
            {
                Thread.Sleep(5000);
                foreach (var node in nodes)
                {
                    var info = node.GetDebugInfo();
                    Assert.That(!string.IsNullOrEmpty(info.Id));
                }
            }
        }

        public void AssertNodesContainFile(ContentId cid, ICodexNodeGroup nodes)
        {
            AssertNodesContainFile(cid, nodes.ToArray());
        }

        public void AssertNodesContainFile(ContentId cid, params ICodexNode[] nodes)
        {
            foreach (var node in nodes)
            {
                var localDatasets = node.LocalFiles();
                CollectionAssert.Contains(localDatasets.Content.Select(c => c.Cid), cid);
            }
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
