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
        
        //[Test]
        //public void TwoClientTest()
        //{
        //    var primary = SetupCodexNode()
        //                        .WithLogLevel(CodexLogLevel.Warn)
        //                        .WithStorageQuota(1024 * 1024)
        //                        .BringOnline();

        //    var secondary = SetupCodexNode()
        //                        .WithBootstrapNode(primary)
        //                        .BringOnline();

        //    var testFile = GenerateTestFile(1024 * 1024);

        //    var contentId = primary.UploadFile(testFile);

        //    var downloadedFile = secondary.DownloadContent(contentId);

        //    testFile.AssertIsEqual(downloadedFile);

        //    // Test files are automatically deleted.
        //    // Online nodes are automatically destroyed.
        //}
    }
}
