using DistTestCore;
using NUnit.Framework;

namespace TestsLong.BasicTests
{
    [TestFixture]
    public class LargeFileTests : DistTest
    {
        [Test, UseLongTimeouts]
        public void OneClientLargeFileTest()
        {
            var primary = SetupCodexNode(s => s
                                .WithStorageQuota(20.GB()));

            var testFile = GenerateTestFile(10.GB());

            var contentId = primary.UploadFile(testFile);

            var downloadedFile = primary.DownloadContent(contentId);

            testFile.AssertIsEqual(downloadedFile);
        }
    }
}
