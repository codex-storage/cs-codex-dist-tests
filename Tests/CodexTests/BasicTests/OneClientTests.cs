using CodexPlugin;
using DistTestCore;
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
            var primary = Ci.StartCodexNode();

            PerformOneClientTest(primary);
        }

        [Test]
        public void RestartTest()
        {
            var primary = Ci.StartCodexNode();

            primary.Stop();

            primary = Ci.StartCodexNode();

            PerformOneClientTest(primary);
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
