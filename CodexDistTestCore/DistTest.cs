using CodexDistTestCore.Config;
using NUnit.Framework;

namespace CodexDistTestCore
{
    [SetUpFixture]
    public abstract class DistTest
    {
        private TestLog log = null!;
        private FileManager fileManager = null!;
        private K8sManager k8sManager = null!;

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
                IncludeLogsOnTestFailure();
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

        private void IncludeLogsOnTestFailure()
        {
            var result = TestContext.CurrentContext.Result;
            if (result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                k8sManager.ForEachOnlineGroup(DownloadLogs);
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
    }

    public static class GlobalTestFailure
    {
        public static bool HasFailed { get; set; } = false;
    }
}
