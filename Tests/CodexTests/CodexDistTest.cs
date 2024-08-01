using CodexContractsPlugin;
using CodexNetDeployer;
using CodexPlugin;
using CodexPlugin.OverwatchSupport;
using CodexTests.Helpers;
using Core;
using DistTestCore;
using DistTestCore.Helpers;
using DistTestCore.Logs;
using Logging;
using MetricsPlugin;
using Newtonsoft.Json;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using OverwatchTranscript;

namespace CodexTests
{
    public class CodexDistTest : DistTest
    {
        private static readonly Dictionary<TestLifecycle, CodexTranscriptWriter> writers = new Dictionary<TestLifecycle, CodexTranscriptWriter>();

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
            base.LifecycleStart(lifecycle);
            SetupTranscript(lifecycle);
        }

        protected override void LifecycleStop(TestLifecycle lifecycle, DistTestResult result)
        {
            base.LifecycleStop(lifecycle, result);
            TeardownTranscript(lifecycle, result);
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

        private CreateTranscriptAttribute? GetTranscriptAttributeOfCurrentTest()
        {
            var attrs = GetCurrentTestMethodAttribute<CreateTranscriptAttribute>();
            if (attrs.Any()) return attrs.Single();
            return null;
        }

        private void SetupTranscript(TestLifecycle lifecycle)
        {
            if (GetTranscriptAttributeOfCurrentTest() == null) return;

            var log = new LogPrefixer(lifecycle.Log, "(Transcript) ");
            var writer = new CodexTranscriptWriter(log, Transcript.NewWriter());
            Ci.SetCodexHooksProvider(writer);
            writers.Add(lifecycle, writer);
        }

        private void TeardownTranscript(TestLifecycle lifecycle, DistTestResult result)
        {
            var attr = GetTranscriptAttributeOfCurrentTest();
            if (attr == null) return;

            var outputFilepath = GetOutputFullPath(lifecycle, attr);

            var writer = writers[lifecycle];
            writers.Remove(lifecycle);

            writer.AddResult(result.Success, result.Result);

            try
            {
                Stopwatch.Measure(lifecycle.Log, "Transcript.ProcessLogs", () =>
                {
                    writer.ProcessLogs(lifecycle.DownloadAllLogs());
                });

                Stopwatch.Measure(lifecycle.Log, $"Transcript.Finalize: {outputFilepath}", () =>
                {
                    writer.IncludeFile(lifecycle.Log.LogFile.FullFilename);
                    writer.Finalize(outputFilepath);
                });
            }
            catch (Exception ex)
            {
                lifecycle.Log.Error("Failure during transcript teardown: " + ex);
            }
        }

        private string GetOutputFullPath(TestLifecycle lifecycle, CreateTranscriptAttribute attr)
        {
            var outputPath = Path.GetDirectoryName(lifecycle.Log.LogFile.FullFilename);
            if (outputPath == null) throw new Exception("Logfile path is null");
            var filename = Path.GetFileNameWithoutExtension(lifecycle.Log.LogFile.FullFilename);
            if (string.IsNullOrEmpty(filename)) throw new Exception("Logfile name is null or empty");
            var outputFile = Path.Combine(outputPath, filename + "_" + attr.OutputFilename);
            if (!outputFile.EndsWith(".owts")) outputFile += ".owts";
            return outputFile;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CreateTranscriptAttribute : PropertyAttribute
    {
        public CreateTranscriptAttribute(string outputFilename)
        {
            OutputFilename = outputFilename;
        }

        public string OutputFilename { get; }
    }
}
