using CodexPlugin;
using DistTestCore;
using NUnit.Framework;
using Utils;

namespace Tests.BasicTests
{
    [TestFixture]
    public class OneClientTests : DistTest
    {
        [Test]
        public void OneClientTest()
        {
            var primary = Ci.SetupCodexNode();

            PerformOneClientTest(primary);
        }

        [Test]
        public void RestartTest()
        {
            var primary = Ci.SetupCodexNode();

            primary.Stop();

            primary = Ci.SetupCodexNode();

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
