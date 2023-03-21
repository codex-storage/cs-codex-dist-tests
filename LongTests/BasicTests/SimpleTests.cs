using CodexDistTestCore;
using NUnit.Framework;

namespace LongTests.BasicTests
{
    [TestFixture]
    public class SimpleTests : DistTest
    {
        [Test, UseLongTimeouts]
        public void OneClientLargeFileTest()
        {
            var primary = SetupCodexNode()
                                .WithLogLevel(CodexLogLevel.Warn)
                                .WithStorageQuota(10.GB())
                                .BringOnline();

            var testFile = GenerateTestFile(1.GB());

            var contentId = primary.UploadFile(testFile);

            var downloadedFile = primary.DownloadContent(contentId);

            testFile.AssertIsEqual(downloadedFile);
        }
    }
}
