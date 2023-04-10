using CodexDistTestCore.Config;
using NUnit.Framework;

namespace CodexDistTestCore
{
    [SetUpFixture]
    public abstract class DistTest
    {
        private TestLog log = null!;
        private FileManager fileManager = null!;
        public K8sManager k8sManager = null!;

        [OneTimeSetUp]
        public void GlobalSetup()
        {
            // Previous test run may have been interrupted.
            // Begin by cleaning everything up.
            log = new TestLog();
            fileManager = new FileManager(log);
            k8sManager = new K8sManager(log, fileManager);

            try
            {
                k8sManager.DeleteAllResources();
                fileManager.DeleteAllTestFiles();
            }
            catch (Exception ex)
            {
                GlobalTestFailure.HasFailed = true;
                log.Error($"Global setup cleanup failed with: {ex}");
                throw;
            }
            log.Log("Global setup cleanup successful");
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
                var dockerImage = new CodexDockerImage();
                log = new TestLog();
                log.Log($"Using docker image '{dockerImage.GetImageTag()}'");

                fileManager = new FileManager(log);
                k8sManager = new K8sManager(log, fileManager);
            }
        }

        [TearDown]
        public void TearDownDistTest()
        {
            try
            {
                log.EndTest();
                IncludeLogsAndMetricsOnTestFailure();
                k8sManager.DeleteAllResources();
                fileManager.DeleteAllTestFiles();
            }
            catch (Exception ex)
            {
                log.Error("Cleanup failed: " + ex.Message);
                GlobalTestFailure.HasFailed = true;
            }
        }

        public TestFile GenerateTestFile(ByteSize size)
        {
            return fileManager.GenerateTestFile(size);
        }

        public IOfflineCodexNodes SetupCodexNodes(int numberOfNodes)
        {
            return new OfflineCodexNodes(k8sManager, numberOfNodes);
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
