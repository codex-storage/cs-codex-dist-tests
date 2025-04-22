﻿using BlockchainUtils;
using CodexClient;
using CodexClient.Hooks;
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
    public class CodexLogTrackerProvider  : ICodexHooksProvider
    {
        private readonly Action<ICodexNode> addNode;

        public CodexLogTrackerProvider(Action<ICodexNode> addNode)
        {
            this.addNode = addNode;
        }

        // See TestLifecycle.cs DownloadAllLogs()
        public ICodexNodeHooks CreateHooks(string nodeName)
        {
            return new CodexLogTracker(addNode);
        }

        public class CodexLogTracker : ICodexNodeHooks
        {
            private readonly Action<ICodexNode> addNode;

            public CodexLogTracker(Action<ICodexNode> addNode)
            {
                this.addNode = addNode;
            }

            public void OnFileDownloaded(ByteSize size, ContentId cid)
            {
            }

            public void OnFileDownloading(ContentId cid)
            {
            }

            public void OnFileUploaded(string uid, ByteSize size, ContentId cid)
            {
            }

            public void OnFileUploading(string uid, ByteSize size)
            {
            }

            public void OnNodeStarted(ICodexNode node, string peerId, string nodeId)
            {
                addNode(node);
            }

            public void OnNodeStarting(DateTime startUtc, string image, EthAccount? ethAccount)
            {
            }

            public void OnNodeStopping()
            {
            }

            public void OnStorageAvailabilityCreated(StorageAvailability response)
            {
            }

            public void OnStorageContractSubmitted(StoragePurchaseContract storagePurchaseContract)
            {
            }

            public void OnStorageContractUpdated(StoragePurchase purchaseStatus)
            {
            }
        }
    }

    public class CodexDistTest : DistTest
    {
        private static readonly object _lock = new object();
        private static readonly Dictionary<TestLifecycle, CodexTranscriptWriter> writers = new Dictionary<TestLifecycle, CodexTranscriptWriter>();
        private static readonly Dictionary<TestLifecycle, BlockCache> blockCaches = new Dictionary<TestLifecycle, BlockCache>();

        // this entire structure is not good and needs to be destroyed at the earliest convenience:
        private static readonly Dictionary<TestLifecycle, List<ICodexNode>> nodes = new Dictionary<TestLifecycle, List<ICodexNode>>();

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

            Ci.AddCodexHooksProvider(new CodexLogTrackerProvider(n =>
            {
                lock (_lock)
                {
                    if (!nodes.ContainsKey(lifecycle)) nodes.Add(lifecycle, new List<ICodexNode>());
                    nodes[lifecycle].Add(n);
                }
            }));
        }

        protected override void LifecycleStop(TestLifecycle lifecycle, DistTestResult result)
        {
            base.LifecycleStop(lifecycle, result);
            TeardownTranscript(lifecycle, result);

            if (!result.Success)
            {
                lock (_lock)
                {
                    var codexNodes = nodes[lifecycle];
                    foreach (var node in codexNodes) node.DownloadLog();
                }
            }
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

        private void SetupTranscript(TestLifecycle lifecycle)
        {
            var attr = GetTranscriptAttributeOfCurrentTest();
            if (attr == null) return;

            var config = new CodexTranscriptWriterConfig(
                attr.IncludeBlockReceivedEvents
            );

            var log = new LogPrefixer(lifecycle.Log, "(Transcript) ");
            var writer = new CodexTranscriptWriter(log, config, Transcript.NewWriter(log));
            Ci.AddCodexHooksProvider(writer);
            lock (_lock)
            {
                writers.Add(lifecycle, writer);
            }
        }

        private void TeardownTranscript(TestLifecycle lifecycle, DistTestResult result)
        {
            var attr = GetTranscriptAttributeOfCurrentTest();
            if (attr == null) return;

            var outputFilepath = GetOutputFullPath(lifecycle, attr);

            CodexTranscriptWriter writer = null!;
            lock (_lock)
            {
                writer = writers[lifecycle];
                writers.Remove(lifecycle);
            }

            writer.AddResult(result.Success, result.Result);

            try
            {
                Stopwatch.Measure(lifecycle.Log, "Transcript.ProcessLogs", () =>
                {
                    writer.ProcessLogs(lifecycle.DownloadAllLogs());
                });

                Stopwatch.Measure(lifecycle.Log, $"Transcript.Finalize: {outputFilepath}", () =>
                {
                    writer.IncludeFile(lifecycle.Log.GetFullName());
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
            var outputPath = Path.GetDirectoryName(lifecycle.Log.GetFullName());
            if (outputPath == null) throw new Exception("Logfile path is null");
            var filename = Path.GetFileNameWithoutExtension(lifecycle.Log.GetFullName());
            if (string.IsNullOrEmpty(filename)) throw new Exception("Logfile name is null or empty");
            var outputFile = Path.Combine(outputPath, filename + "_" + attr.OutputFilename);
            if (!outputFile.EndsWith(".owts")) outputFile += ".owts";
            return outputFile;
        }

        private BlockCache GetBlockCache()
        {
            var lifecycle = Get();
            lock (_lock)
            {
                if (!blockCaches.ContainsKey(lifecycle))
                {
                    blockCaches[lifecycle] = new BlockCache();
                }
            }
            return blockCaches[lifecycle];
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
