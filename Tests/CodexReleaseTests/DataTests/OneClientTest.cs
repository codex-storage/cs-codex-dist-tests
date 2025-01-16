using CodexClient;
using CodexTests;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.DataTests
{
    [TestFixture]
    public class OneClientTest : CodexDistTest
    {
        [Test]
        public void OneClient()
        {
            var node = StartCodex();

            PerformOneClientTest(node);

            LogNodeStatus(node);
        }

        private void PerformOneClientTest(ICodexNode primary)
        {
            var testFile = GenerateTestFile(1.MB());

            var contentId = primary.UploadFile(testFile);

            AssertNodesContainFile(contentId, primary);

            var downloadedFile = primary.DownloadContent(contentId);

            testFile.AssertIsEqual(downloadedFile);
        }
    }
}
