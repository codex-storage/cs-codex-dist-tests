using DistTestCore.Codex;
using DistTestCore.Logs;
using DistTestCore.Marketplace;
using DistTestCore.Metrics;
using KubernetesWorkflow;
using Logging;
using NUnit.Framework;
using System.Reflection;
using Utils;

namespace DistTestCore
{
    [SetUpFixture]
    public abstract class DistTest
    {
        private readonly Configuration configuration = new Configuration();
        private readonly Assembly[] testAssemblies;
        private FixtureLog fixtureLog = null!;
        private TestLifecycle lifecycle = null!;
        private DateTime testStart = DateTime.MinValue;

        public DistTest()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            testAssemblies = assemblies.Where(a => a.FullName!.ToLowerInvariant().Contains("test")).ToArray();
        }

        [OneTimeSetUp]
        public void GlobalSetup()
        {
            // Previous test run may have been interrupted.
            // Begin by cleaning everything up.
            Timing.UseLongTimeouts = false;
            fixtureLog = new FixtureLog(configuration.GetLogConfig());

            try
            {
                Stopwatch.Measure(fixtureLog, "Global setup", () =>
                {
                    var wc = new WorkflowCreator(fixtureLog, configuration.GetK8sConfiguration());
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
            Timing.UseLongTimeouts = ShouldUseLongTimeouts();

            if (GlobalTestFailure.HasFailed)
            {
                Assert.Inconclusive("Skip test: Previous test failed during clean up.");
            }
            else
            {
                CreateNewTestLifecycle();
            }
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
            return lifecycle.FileManager.GenerateTestFile(size);
        }

        public IOnlineCodexNode SetupCodexBootstrapNode()
        {
            return SetupCodexBootstrapNode(s => { });
        }

        public IOnlineCodexNode SetupCodexBootstrapNode(Action<ICodexSetup> setup)
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

        public ICodexNodeGroup SetupCodexNodes(int numberOfNodes, Action<ICodexSetup> setup)
        {
            var codexSetup = new CodexSetup(numberOfNodes);

            setup(codexSetup);

            return BringOnline(codexSetup);
        }

        public ICodexNodeGroup BringOnline(ICodexSetup codexSetup)
        {
            return lifecycle.CodexStarter.BringOnline((CodexSetup)codexSetup);
        }

        protected BaseLog Log
        {
            get { return lifecycle.Log; }
        }

        private void CreateNewTestLifecycle()
        {
            Stopwatch.Measure(fixtureLog, $"Setup for {GetCurrentTestName()}", () =>
            {
                lifecycle = new TestLifecycle(fixtureLog.CreateTestLog(), configuration);
                testStart = DateTime.UtcNow;
            });
        }

        private void DisposeTestLifecycle()
        {
            fixtureLog.Log($"{GetCurrentTestName()} = {GetTestResult()} ({GetTestDuration()})");
            Stopwatch.Measure(fixtureLog, $"Teardown for {GetCurrentTestName()}", () =>
            {
                lifecycle.Log.EndTest();
                IncludeLogsAndMetricsOnTestFailure();
                lifecycle.DeleteAllResources();
                lifecycle = null!;
            });
        }

        private void IncludeLogsAndMetricsOnTestFailure()
        {
            var result = TestContext.CurrentContext.Result;
            if (result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                fixtureLog.MarkAsFailed();

                if (IsDownloadingLogsAndMetricsEnabled())
                {
                    lifecycle.Log.Log("Downloading all CodexNode logs and metrics because of test failure...");
                    DownloadAllLogs();
                    DownloadAllMetrics();
                }
                else
                {
                    lifecycle.Log.Log("Skipping download of all CodexNode logs and metrics due to [DontDownloadLogsAndMetricsOnFailure] attribute.");
                }
            }
        }

        private string GetTestDuration()
        {
            var testDuration = DateTime.UtcNow - testStart;
            return Time.FormatDuration(testDuration);
        }

        private void DownloadAllLogs()
        {
            OnEachCodexNode(node =>
            {
                lifecycle.DownloadLog(node);
            });
        }

        private void DownloadAllMetrics()
        {
            var metricsDownloader = new MetricsDownloader(lifecycle.Log);

            OnEachCodexNode(node =>
            {
                var m = node.Metrics as MetricsAccess;
                if (m != null)
                {
                    metricsDownloader.DownloadAllMetricsForNode(node.GetName(), m);
                }
            });
        }

        private void OnEachCodexNode(Action<OnlineCodexNode> action)
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
