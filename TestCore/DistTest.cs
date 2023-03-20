using NUnit.Framework;

namespace CodexDistTests.TestCore
{
    public abstract class DistTest
    {
        private FileManager fileManager = null!;
        private K8sManager k8sManager = null!;

        [SetUp]
        public void SetUpDistTest()
        {
            if (GlobalTestFailure.HasFailed)
            {
                Assert.Inconclusive("Skip test: Previous test failed during clean up.");
            }
            else
            {
                fileManager = new FileManager();
                k8sManager = new K8sManager(fileManager);
            }
        }

        [TearDown]
        public void TearDownDistTest()
        {
            try
            {
                k8sManager.DeleteAllResources();
                fileManager.DeleteAllTestFiles();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cleanup has failed." + ex.Message);
                GlobalTestFailure.HasFailed = true;
            }
        }

        public TestFile GenerateTestFile(int size = 1024)
        {
            return fileManager.GenerateTestFile(size);
        }

        public IOfflineCodexNode SetupCodexNode()
        {
            return new OfflineCodexNode(k8sManager);
        }
    }

    public static class GlobalTestFailure
    {
        public static bool HasFailed { get; set; } = false;
    }
}
