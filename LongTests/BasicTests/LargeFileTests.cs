using DistTestCore;
using DistTestCore.Codex;
using NUnit.Framework;

namespace TestsLong.BasicTests
{
    [TestFixture]
    public class LargeFileTests : DistTest
    {
        [Test, UseLongTimeouts]
        public void OneClientLargeFileTest()
        {
            var primary = SetupCodexNodes(1)
                                .WithLogLevel(CodexLogLevel.Warn)
                                .WithStorageQuota(20.GB())
                                .BringOnline()[0];

            var testFile = GenerateTestFile(10.GB());

            var contentId = primary.UploadFile(testFile);

            var downloadedFile = primary.DownloadContent(contentId);

            testFile.AssertIsEqual(downloadedFile);
        }
    }
}
