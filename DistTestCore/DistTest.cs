using DistTestCore.Codex;
using DistTestCore.Logs;
using DistTestCore.Marketplace;
using DistTestCore.Metrics;
using KubernetesWorkflow;
using Logging;
using NUnit.Framework;
using System.Reflection;

namespace DistTestCore
{
    [SetUpFixture]
    [Parallelizable(ParallelScope.All)]
    public abstract class DistTest
    {
        private readonly Configuration configuration = new Configuration();
        private readonly Assembly[] testAssemblies;
        private readonly FixtureLog fixtureLog;
        private readonly object lifecycleLock = new object();
        private readonly Dictionary<string, TestLifecycle> lifecycles = new Dictionary<string, TestLifecycle>();

        public DistTest()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            testAssemblies = assemblies.Where(a => a.FullName!.ToLowerInvariant().Contains("test")).ToArray();

            fixtureLog = new FixtureLog(configuration.GetLogConfig());
        }

        [OneTimeSetUp]
        public void GlobalSetup()
        {
            // Previous test run may have been interrupted.
            // Begin by cleaning everything up.
            try
            {
                Stopwatch.Measure(fixtureLog, "Global setup", () =>
                {
                    var wc = new WorkflowCreator(fixtureLog, configuration.GetK8sConfiguration(GetTimeSet()));
                    wc.CreateWorkflow().DeleteAllResources();
                });                
            }
            catch (Exception ex)
            {
                GlobalTestFailure.HasFailed = true;
                fixtureLog.Error($"Global setup cleanup failed with: {ex}");
                throw;
            }

            fixtureLog.Log("Global setup cleanup successful");
            fixtureLog.Log($"Codex image: '{CodexContainerRecipe.DockerImage}'");
            fixtureLog.Log($"Prometheus image: '{PrometheusContainerRecipe.DockerImage}'");
            fixtureLog.Log($"Geth image: '{GethContainerRecipe.DockerImage}'");
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

        public TestFile GenerateTestFile(ByteSize size)
        {
            return Get().FileManager.GenerateTestFile(size);
        }

        public IOnlineCodexNode SetupCodexBootstrapNode()
        {
            return SetupCodexBootstrapNode(s => { });
        }

        public virtual IOnlineCodexNode SetupCodexBootstrapNode(Action<ICodexSetup> setup)
        {
            return SetupCodexNode(s =>
            {
                setup(s);
                s.WithName("Bootstrap");
            });
        }

        public IOnlineCodexNode SetupCodexNode()
        {
            return SetupCodexNode(s => { });
        }

        public IOnlineCodexNode SetupCodexNode(Action<ICodexSetup> setup)
        {
            return SetupCodexNodes(1, setup)[0];
        }

        public ICodexNodeGroup SetupCodexNodes(int numberOfNodes)
        {
            return SetupCodexNodes(numberOfNodes, s => { });
        }

        public virtual ICodexNodeGroup SetupCodexNodes(int numberOfNodes, Action<ICodexSetup> setup)
        {
            var codexSetup = new CodexSetup(numberOfNodes);

            setup(codexSetup);

            return BringOnline(codexSetup);
        }

        public ICodexNodeGroup BringOnline(ICodexSetup codexSetup)
        {
            return Get().CodexStarter.BringOnline((CodexSetup)codexSetup);
        }

        protected BaseLog Log
        {
            get { return Get().Log; }
        }

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
            Stopwatch.Measure(fixtureLog, $"Setup for {testName}", () =>
            {
                lock (lifecycleLock)
                {
                    lifecycles.Add(testName, new TestLifecycle(fixtureLog.CreateTestLog(), configuration, GetTimeSet()));
                }
            });
        }

        private void DisposeTestLifecycle()
        {
            var lifecycle = Get();
            fixtureLog.Log($"{GetCurrentTestName()} = {GetTestResult()} ({lifecycle.GetTestDuration()})");
            Stopwatch.Measure(fixtureLog, $"Teardown for {GetCurrentTestName()}", () =>
            {
                lifecycle.Log.EndTest();
                IncludeLogsAndMetricsOnTestFailure(lifecycle);
                lifecycle.DeleteAllResources();
                lifecycle = null!;
            });
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
                    DownloadAllLogs(lifecycle);
                    DownloadAllMetrics(lifecycle);
                }
                else
                {
                    lifecycle.Log.Log("Skipping download of all CodexNode logs and metrics due to [DontDownloadLogsAndMetricsOnFailure] attribute.");
                }
            }
        }

        private void DownloadAllLogs(TestLifecycle lifecycle)
        {
            OnEachCodexNode(lifecycle, node =>
            {
                lifecycle.DownloadLog(node);
            });
        }

        private void DownloadAllMetrics(TestLifecycle lifecycle)
        {
            var metricsDownloader = new MetricsDownloader(lifecycle.Log);

            OnEachCodexNode(lifecycle, node =>
            {
                var m = node.Metrics as MetricsAccess;
                if (m != null)
                {
                    metricsDownloader.DownloadAllMetricsForNode(node.GetName(), m);
                }
            });
        }

        private void OnEachCodexNode(TestLifecycle lifecycle, Action<OnlineCodexNode> action)
        {
            var allNodes = lifecycle.CodexStarter.RunningGroups.SelectMany(g => g.Nodes);
            foreach (var node in allNodes)
            {
                action(node);
            }
        }

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
