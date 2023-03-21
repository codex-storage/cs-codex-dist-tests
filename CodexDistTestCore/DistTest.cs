using NUnit.Framework;

namespace CodexDistTestCore
{
    [SetUpFixture]
    public abstract class DistTest
    {
        private FileManager fileManager = null!;
        private K8sManager k8sManager = null!;

        [OneTimeSetUp]
        public void GlobalSetup()
        {
            // Previous test run may have been interrupted.
            // Begin by cleaning everything up.
            fileManager = new FileManager();
            k8sManager = new K8sManager(fileManager);

            k8sManager.DeleteAllResources();
            fileManager.DeleteAllTestFiles();
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
                TestLog.BeginTest();
                fileManager = new FileManager();
                k8sManager = new K8sManager(fileManager);
            }
        }

        [TearDown]
        public void TearDownDistTest()
        {
            try
            {
                TestLog.EndTest(k8sManager);
                k8sManager.DeleteAllResources();
                fileManager.DeleteAllTestFiles();
            }
            catch (Exception ex)
            {
                TestLog.Error("Cleanup failed: " + ex.Message);
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
    }

    public static class GlobalTestFailure
    {
        public static bool HasFailed { get; set; } = false;
    }
}
