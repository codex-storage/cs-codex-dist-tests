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
            var primary = SetupCodexNodes(1).BringOnline()[0];

            PerformOneClientTest(primary);
        }

        [Test]
        public void RestartTest()
        {
            var group = SetupCodexNodes(1).BringOnline();

            var setup = group.BringOffline();

            var primary = setup.BringOnline()[0];

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
