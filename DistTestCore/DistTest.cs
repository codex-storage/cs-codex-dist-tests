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

        public ICodexSetupConfig SetupCodexNodes(int numberOfNodes)
        {
            return new CodexSetupConfig(lifecycle.CodexStarter, numberOfNodes);
        }

        private void IncludeLogsAndMetricsOnTestFailure()
        {
            var result = TestContext.CurrentContext.Result;
            if (result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                if (IsDownloadingLogsAndMetricsEnabled())
                {
                    log.Log("Downloading all CodexNode logs and metrics because of test failure...");
                    k8sManager.ForEachOnlineGroup(DownloadLogs);
                    k8sManager.DownloadAllMetrics();
                }
                else
                {
                    log.Log("Skipping download of all CodexNode logs and metrics due to [DontDownloadLogsAndMetricsOnFailure] attribute.");
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

        private void DownloadLogs(CodexNodeGroup group)
        {
            foreach (var node in group)
            {
                var downloader = new PodLogDownloader(log, k8sManager);
                var n = (OnlineCodexNode)node;
                downloader.DownloadLog(n);
            }
        }

        private bool IsDownloadingLogsAndMetricsEnabled()
        {
            var testProperties = TestContext.CurrentContext.Test.Properties;
            return !testProperties.ContainsKey(PodLogDownloader.DontDownloadLogsOnFailureKey);
        }
    }

    public static class GlobalTestFailure
    {
        public static bool HasFailed { get; set; } = false;
    }
}
