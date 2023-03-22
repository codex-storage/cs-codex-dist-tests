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
            var primary = SetupCodexNodes(1)
                                .WithLogLevel(CodexLogLevel.Warn)
                                .WithStorageQuota(10.GB())
                                .BringOnline()[0];

            var testFile = GenerateTestFile(1.GB());

            var contentId = primary.UploadFile(testFile);

            var downloadedFile = primary.DownloadContent(contentId);

            testFile.AssertIsEqual(downloadedFile);
        }

        [Test, UseLongTimeouts]
        public void TestEnvironmentAddressSpaceTest()
        {
            var group = SetupCodexNodes(1000).BringOnline();

            var nodeIds = group.Select(n => n.GetDebugInfo().id).ToArray();

            Assert.That(nodeIds.Length, Is.EqualTo(nodeIds.Distinct().Count()),
                "Not all created nodes provided a unique id.");
        }
    }
}
