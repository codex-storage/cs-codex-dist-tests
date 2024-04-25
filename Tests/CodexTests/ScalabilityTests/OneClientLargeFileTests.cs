using CodexPlugin;
using DistTestCore;
using NUnit.Framework;
using Utils;

namespace CodexTests.ScalabilityTests
{
    [TestFixture]
    public class OneClientLargeFileTests : CodexDistTest
    {
        [Test]
        [Combinatorial]
        [UseLongTimeouts]
        public void OneClientLargeFile([Values(
            256,
            512,
            1024, // GB
            2048,
            4096,
            8192,
            16384,
            32768,
            65536,
            131072
        )] int sizeMb)
        {
            var testFile = GenerateTestFile(sizeMb.MB());

            var node = AddCodex(s => s
                .WithLogLevel(CodexLogLevel.Warn)
                .WithStorageQuota((sizeMb + 10).MB())
            );
            var contentId = node.UploadFile(testFile);
            var downloadedFile = node.DownloadContent(contentId);

            testFile.AssertIsEqual(downloadedFile);
        }
    }
}
