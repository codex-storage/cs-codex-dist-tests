using DistTestCore;
using NUnit.Framework;

namespace Tests.BasicTests
{
    [TestFixture]
    public class OneClientTests : DistTest
    {
        [Test]
        public void OneClientTest()
        {
            var primary = SetupCodexNode();

            PerformOneClientTest(primary);
        }

        [Test]
        public void RestartTest()
        {
            var primary = SetupCodexNode();

            var setup = primary.BringOffline();

            primary = BringOnline(setup)[0];

            PerformOneClientTest(primary);
        }

        private void PerformOneClientTest(IOnlineCodexNode primary)
        {
            var testFile = GenerateTestFile(1.MB());

            var contentId = primary.UploadFile(testFile);

            var downloadedFile = primary.DownloadContent(contentId);

            testFile.AssertIsEqual(downloadedFile);
        }
    }
}
