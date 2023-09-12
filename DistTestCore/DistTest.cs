using Core;
using FileUtils;
using KubernetesWorkflow;
using Logging;
using NUnit.Framework;
using System.Reflection;
using Utils;

namespace DistTestCore
{
    [Parallelizable(ParallelScope.All)]
    public abstract class DistTest : PluginInterface
    {
        private const string TestsType = "dist-tests";
        private const string TestNamespacePrefix = "ct-";
        private readonly Configuration configuration = new Configuration();
        private readonly Assembly[] testAssemblies;
        private readonly FixtureLog fixtureLog;
        private readonly StatusLog statusLog;
        private readonly object lifecycleLock = new object();
        private readonly Dictionary<string, TestLifecycle> lifecycles = new Dictionary<string, TestLifecycle>();
        private readonly PluginManager PluginManager = new PluginManager();
        
        public DistTest()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            testAssemblies = assemblies.Where(a => a.FullName!.ToLowerInvariant().Contains("test")).ToArray();

            var logConfig = configuration.GetLogConfig();
            var startTime = DateTime.UtcNow;
            fixtureLog = new FixtureLog(logConfig, startTime);
            statusLog = new StatusLog(logConfig, startTime);
        }

        [OneTimeSetUp]
        public void GlobalSetup()
        {
            fixtureLog.Log($"Distributed Tests are starting...");
            PluginManager.DiscoverPlugins();
            AnnouncePlugins(fixtureLog);

            // Previous test run may have been interrupted.
            // Begin by cleaning everything up.
            try
            {
                Stopwatch.Measure(fixtureLog, "Global setup", () =>
                {
                    var wc = new WorkflowCreator(fixtureLog, configuration.GetK8sConfiguration(GetTimeSet()), string.Empty);
                    wc.CreateWorkflow().DeleteNamespacesStartingWith(TestNamespacePrefix);
                });                
            }
            catch (Exception ex)
            {
                GlobalTestFailure.HasFailed = true;
                fixtureLog.Error($"Global setup cleanup failed with: {ex}");
                throw;
            }

            fixtureLog.Log("Global setup cleanup successful");
        }

        [OneTimeTearDown]
        public void GlobalTearDown()
        {
            FinalizePlugins(fixtureLog);
        }

        [SetUp]
        public void SetUpDistTest()
        {
            if (GlobalTestFailure.HasFailed)
            {
                Assert.Inconclusive("Skip test: Previous test failed during clean up.");
            }
            else
            {
                CreateNewTestLifecycle();
            }
        }

        [TearDown]
        public void TearDownDistTest()
        {
            try
            {
                DisposeTestLifecycle();
            }
            catch (Exception ex)
            {
                fixtureLog.Error("Cleanup failed: " + ex.Message);
                GlobalTestFailure.HasFailed = true;
            }
        }

        public TrackedFile GenerateTestFile(ByteSize size, string label = "")
        {
            return Get().FileManager.GenerateTestFile(size, label);
        }

        /// <summary>
        /// Any test files generated in 'action' will be deleted after it returns.
        /// This helps prevent large tests from filling up discs.
        /// </summary>
        public void ScopedTestFiles(Action action)
        {
            Get().FileManager.ScopedFiles(action);
        }

        //public IOnlineCodexNode SetupCodexBootstrapNode()
        //{
        //    return SetupCodexBootstrapNode(s => { });
        //}

        //public virtual IOnlineCodexNode SetupCodexBootstrapNode(Action<ICodexSetup> setup)
        //{
        //    return SetupCodexNode(s =>
        //    {
        //        setup(s);
        //        s.WithName("Bootstrap");
        //    });
        //}

        //public IOnlineCodexNode SetupCodexNode()
        //{
        //    return SetupCodexNode(s => { });
        //}

        //public IOnlineCodexNode SetupCodexNode(Action<ICodexSetup> setup)
        //{
        //    return SetupCodexNodes(1, setup)[0];
        //}

        //public ICodexNodeGroup SetupCodexNodes(int numberOfNodes)
        //{
        //    return SetupCodexNodes(numberOfNodes, s => { });
        //}

        //public virtual ICodexNodeGroup SetupCodexNodes(int numberOfNodes, Action<ICodexSetup> setup)
        //{
        //    var codexSetup = CreateCodexSetup(numberOfNodes);

        //    setup(codexSetup);

        //    return BringOnline(codexSetup);
        //}

        //public ICodexNodeGroup BringOnline(ICodexSetup codexSetup)
        //{
        //    return Get().CodexStarter.BringOnline((CodexSetup)codexSetup);
        //}

        //public IEnumerable<IOnlineCodexNode> GetAllOnlineCodexNodes()
        //{
        //    return Get().CodexStarter.RunningGroups.SelectMany(g => g.Nodes);
        //}

        public override T GetPlugin<T>()
        {
            return Get().GetPlugin<T>();
        }

        private void AnnouncePlugins(FixtureLog fixtureLog)
        {
            PluginManager.AnnouncePlugins(fixtureLog);
        }

        private void FinalizePlugins(FixtureLog fixtureLog)
        {
            PluginManager.FinalizePlugins(fixtureLog);
        }

        public ILog GetTestLog()
        {
            return Get().Log;
        }

        public void Log(string msg)
        {
            TestContext.Progress.WriteLine(msg);
            GetTestLog().Log(msg);
        }

        public void Debug(string msg)
        {
            TestContext.Progress.WriteLine(msg);
            GetTestLog().Debug(msg);
        }

        //public PeerConnectionTestHelpers CreatePeerConnectionTestHelpers()
        //{
        //    return new PeerConnectionTestHelpers(GetTestLog());
        //}

        //public PeerDownloadTestHelpers CreatePeerDownloadTestHelpers()
        //{
        //    return new PeerDownloadTestHelpers(GetTestLog(), Get().FileManager);
        //}

        public void Measure(string name, Action action)
        {
            Stopwatch.Measure(Get().Log, name, action);
        }

        //protected CodexSetup CreateCodexSetup(int numberOfNodes)
        //{
        //    return new CodexSetup(numberOfNodes, configuration.GetCodexLogLevel());
        //}

        private TestLifecycle Get()
        {
            lock (lifecycleLock)
            {
                return lifecycles[GetCurrentTestName()];
            }
        }

        private void CreateNewTestLifecycle()
        {
            var testName = GetCurrentTestName();
            fixtureLog.WriteLogTag();
            Stopwatch.Measure(fixtureLog, $"Setup for {testName}", () =>
            {
                lock (lifecycleLock)
                {
                    var testNamespace = TestNamespacePrefix + Guid.NewGuid().ToString();
                    var lifecycle = new TestLifecycle(fixtureLog.CreateTestLog(), configuration, GetTimeSet(), testNamespace);
                    lifecycles.Add(testName, lifecycle);
                    DefaultContainerRecipe.TestsType = TestsType;
                    //DefaultContainerRecipe.ApplicationIds = lifecycle.GetApplicationIds();
                }
            });
        }

        private void DisposeTestLifecycle()
        {
            var lifecycle = Get();
            var testResult = GetTestResult();
            var testDuration = lifecycle.GetTestDuration();
            fixtureLog.Log($"{GetCurrentTestName()} = {testResult} ({testDuration})");
            statusLog.ConcludeTest(testResult, testDuration);//, lifecycle.GetApplicationIds());
            Stopwatch.Measure(fixtureLog, $"Teardown for {GetCurrentTestName()}", () =>
            {
                WriteEndTestLog(lifecycle.Log);

                IncludeLogsAndMetricsOnTestFailure(lifecycle);
                lifecycle.DeleteAllResources();
                lifecycle = null!;
            });
        }

        private void WriteEndTestLog(TestLog log)
        {
            var result = TestContext.CurrentContext.Result;

            Log($"*** Finished: {GetCurrentTestName()} = {result.Outcome.Status}");
            if (!string.IsNullOrEmpty(result.Message))
            {
                Log(result.Message);
                Log($"{result.StackTrace}");
            }

            if (result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                log.MarkAsFailed();
            }
        }

        private ITimeSet GetTimeSet()
        {
            if (ShouldUseLongTimeouts()) return new LongTimeSet();
            return new DefaultTimeSet();
        }

        private bool ShouldUseLongTimeouts()
        {
            // Don't be fooled! TestContext.CurrentTest.Test allows you easy access to the attributes of the current test.
            // But this doesn't work for tests making use of [TestCase]. So instead, we use reflection here to figure out
            // if the attribute is present.
            var currentTest = TestContext.CurrentContext.Test;
            var className = currentTest.ClassName;
            var methodName = currentTest.MethodName;

            var testClasses = testAssemblies.SelectMany(a => a.GetTypes()).Where(c => c.FullName == className).ToArray();
            var testMethods = testClasses.SelectMany(c => c.GetMethods()).Where(m => m.Name == methodName).ToArray();

            return testMethods.Any(m => m.GetCustomAttribute<UseLongTimeoutsAttribute>() != null);
        }

        private void IncludeLogsAndMetricsOnTestFailure(TestLifecycle lifecycle)
        {
            var result = TestContext.CurrentContext.Result;
            if (result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                fixtureLog.MarkAsFailed();

                if (IsDownloadingLogsAndMetricsEnabled())
                {
                    lifecycle.Log.Log("Downloading all CodexNode logs and metrics because of test failure...");
                    //DownloadAllLogs(lifecycle);
                    //DownloadAllMetrics(lifecycle);
                }
                else
                {
                    lifecycle.Log.Log("Skipping download of all CodexNode logs and metrics due to [DontDownloadLogsAndMetricsOnFailure] attribute.");
                }
            }
        }

        //private void DownloadAllLogs(TestLifecycle lifecycle)
        //{
        //    OnEachCodexNode(lifecycle, node =>
        //    {
        //        lifecycle.DownloadLog(node.CodexAccess.Container);
        //    });
        //}

        //private void DownloadAllMetrics(TestLifecycle lifecycle)
        //{
        //    var metricsDownloader = new MetricsDownloader(lifecycle.Log);

        //    OnEachCodexNode(lifecycle, node =>
        //    {
        //        var m = node.Metrics as MetricsAccess;
        //        if (m != null)
        //        {
        //            metricsDownloader.DownloadAllMetricsForNode(node.GetName(), m);
        //        }
        //    });
        //}

        //private void OnEachCodexNode(TestLifecycle lifecycle, Action<OnlineCodexNode> action)
        //{
        //    var allNodes = lifecycle.CodexStarter.RunningGroups.SelectMany(g => g.Nodes);
        //    foreach (var node in allNodes)
        //    {
        //        action(node);
        //    }
        //}

        private string GetCurrentTestName()
        {
            return $"[{TestContext.CurrentContext.Test.Name}]";
        }

        private string GetTestResult()
        {
            return TestContext.CurrentContext.Result.Outcome.Status.ToString();
        }

        private bool IsDownloadingLogsAndMetricsEnabled()
        {
            var testProperties = TestContext.CurrentContext.Test.Properties;
            return !testProperties.ContainsKey(DontDownloadLogsAndMetricsOnFailureAttribute.DontDownloadKey);
        }
    }

    public static class GlobalTestFailure
    {
        public static bool HasFailed { get; set; } = false;
    }
}
