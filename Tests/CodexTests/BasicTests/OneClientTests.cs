using CodexPlugin;
using NUnit.Framework;
using Utils;

namespace CodexTests.BasicTests
{
    [TestFixture]
    public class OneClientTests : CodexDistTest
    {
        [Test]
        public void OneClientTest()
        {
            var primary = StartCodex();

            PerformOneClientTest(primary);

            LogNodeStatus(primary);
        }

        private void PerformOneClientTest(ICodexNode primary)
        {
            var testFile = GenerateTestFile(1.MB());

            var contentId = primary.UploadFile(testFile);

            var downloadedFile = primary.DownloadContent(contentId);

            testFile.AssertIsEqual(downloadedFile);
        }
    }
}
