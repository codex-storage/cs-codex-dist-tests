using DistTestCore.Codex;
using DistTestCore.Logs;
using DistTestCore.Marketplace;
using DistTestCore.Metrics;
using KubernetesWorkflow;
using Logging;
using NUnit.Framework;
using Utils;

namespace DistTestCore
{
    [SetUpFixture]
    public abstract class DistTest
    {
        private readonly Configuration configuration = new Configuration();
        private FixtureLog fixtureLog = null!;
        private TestLifecycle lifecycle = null!;
        private DateTime testStart = DateTime.MinValue;

        [OneTimeSetUp]
        public void GlobalSetup()
        {
            // Previous test run may have been interrupted.
            // Begin by cleaning everything up.
            fixtureLog = new FixtureLog(configuration.GetLogConfig());

            try
            {
                Stopwatch.Measure(fixtureLog, "Global setup", () =>
                {
                    var wc = new WorkflowCreator(configuration.GetK8sConfiguration());
                    wc.CreateWorkflow().DeleteAllResources();
                });                
            }
            catch (Exception ex)
            {
                GlobalTestFailure.HasFailed = true;
                Error($"Global setup cleanup failed with: {ex}");
                throw;
            }

            fixtureLog.Log("Global setup cleanup successful");
            fixtureLog.Log($"Codex image: {CodexContainerRecipe.DockerImage}");
            fixtureLog.Log($"Prometheus image: {PrometheusContainerRecipe.DockerImage}");
            fixtureLog.Log($"Geth image: {GethContainerRecipe.DockerImage}");
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
                Error("Cleanup failed: " + ex.Message);
                GlobalTestFailure.HasFailed = true;
            }
        }

        public TestFile GenerateTestFile(ByteSize size)
        {
            return lifecycle.FileManager.GenerateTestFile(size);
        }

        public ICodexSetup SetupCodexNodes(int numberOfNodes)
        {
            return new CodexSetup(lifecycle.CodexStarter, numberOfNodes);
        }

        private void IncludeLogsAndMetricsOnTestFailure()
        {
            var result = TestContext.CurrentContext.Result;
            if (result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                fixtureLog.MarkAsFailed();

                if (IsDownloadingLogsAndMetricsEnabled())
                {
                    Log("Downloading all CodexNode logs and metrics because of test failure...");
                    DownloadAllLogs();
                    DownloadAllMetrics();
                }
                else
                {
                    Log("Skipping download of all CodexNode logs and metrics due to [DontDownloadLogsAndMetricsOnFailure] attribute.");
                }
            }
        }

        private void Log(string msg)
        {
            lifecycle.Log.Log(msg);
        }

        private void Error(string msg)
        {
            lifecycle.Log.Error(msg);
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
