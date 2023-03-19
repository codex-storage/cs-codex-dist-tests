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
            fileManager = new FileManager();
            k8sManager = new K8sManager(fileManager);
        }

        [TearDown]
        public void TearDownDistTest()
        {
            k8sManager.DeleteAllResources();
            fileManager.DeleteAllTestFiles();
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
}
