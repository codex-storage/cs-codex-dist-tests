using DistTestCore.CodexLogsAndMetrics;
using DistTestCore.Metrics;
using NUnit.Framework;

namespace DistTestCore
{
    [SetUpFixture]
    public abstract class DistTest
    {
        private TestLifecycle lifecycle = null!;

        [OneTimeSetUp]
        public void GlobalSetup()
        {
            // Previous test run may have been interrupted.
            // Begin by cleaning everything up.
            CreateNewTestLifecycle();

            try
            {
                lifecycle.DeleteAllResources();
            }
            catch (Exception ex)
            {
                GlobalTestFailure.HasFailed = true;
                Error($"Global setup cleanup failed with: {ex}");
                throw;
            }
            Log("Global setup cleanup successful");
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
                lifecycle.Log.EndTest();
                IncludeLogsAndMetricsOnTestFailure();
                lifecycle.DeleteAllResources();
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
            lifecycle = new TestLifecycle(new Configuration());
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
                var m = (MetricsAccess)node.Metrics;
                metricsDownloader.DownloadAllMetricsForNode(node.GetName(), m);
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
