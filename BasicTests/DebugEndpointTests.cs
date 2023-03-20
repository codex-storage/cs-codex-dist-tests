using CodexDistTests.TestCore;
using NUnit.Framework;

namespace CodexDistTests.BasicTests
{
    [TestFixture]
    public class DebugEndpointTests : DistTest
    {
        [Test]
        public void GetDebugInfo()
        {
            var node = SetupCodexNode().BringOnline();

            var debugInfo = node.GetDebugInfo();

            Assert.That(debugInfo.spr, Is.Not.Empty);
        }

        [Test]
        public void OneClientTest()
        {
            var primary = SetupCodexNode()
                                .WithLogLevel(CodexLogLevel.Trace)
                                .WithStorageQuota(1024 * 1024 * 2)
                                .BringOnline();

            var testFile = GenerateTestFile(1024 * 1024);

            var contentId = primary.UploadFile(testFile);

            var downloadedFile = primary.DownloadContent(contentId);

            testFile.AssertIsEqual(downloadedFile);
        }

        //[Test]
        //public void TwoClientTest()
        //{
        //    var primary = SetupCodexNode()
        //                        .WithLogLevel(CodexLogLevel.Trace)
        //                        .WithStorageQuota(1024 * 1024 * 2)
        //                        .BringOnline();

        //    var secondary = SetupCodexNode()
        //                        .WithLogLevel(CodexLogLevel.Trace)
        //                        .WithBootstrapNode(primary)
        //                        .BringOnline();

        //    var testFile = GenerateTestFile(1024 * 1024);

        //    var contentId = primary.UploadFile(testFile);

        //    var downloadedFile = secondary.DownloadContent(contentId);

        //    testFile.AssertIsEqual(downloadedFile);
        //}
    }
}
