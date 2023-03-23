using CodexDistTestCore;
using NUnit.Framework;

namespace Tests.BasicTests
{
    [TestFixture]
    public class SimpleTests : DistTest
    {
        [Test]
        public void GetDebugInfo()
        {
            var dockerImage = new CodexDockerImage();

            var node = SetupCodexNodes(1).BringOnline()[0];

            var debugInfo = node.GetDebugInfo();

            Assert.That(debugInfo.spr, Is.Not.Empty);
            Assert.That(debugInfo.codex.revision, Is.EqualTo(dockerImage.GetExpectedImageRevision()));
        }

        [Test]
        public void OneClientTest()
        {
            var primary = SetupCodexNodes(1)
                            .WithLogLevel(CodexLogLevel.Trace)
                            .WithStorageQuota(2.MB())
                            .BringOnline()[0];

            var testFile = GenerateTestFile(1.MB());

            var contentId = primary.UploadFile(testFile);

            var downloadedFile = primary.DownloadContent(contentId);

            testFile.AssertIsEqual(downloadedFile);
        }

        [Test]
        public void TwoClientOnePodTest()
        {
            var group = SetupCodexNodes(2)
                        .WithLogLevel(CodexLogLevel.Trace)
                        .WithStorageQuota(2.MB())
                        .BringOnline();

            var primary = group[0];
            var secondary = group[1];

            PerformTwoClientTest(primary, secondary);
        }

        [Test]
        public void TwoClientTwoPodTest()
        {
            var primary = SetupCodexNodes(1)
                            .WithStorageQuota(2.MB())
                            .BringOnline()[0];

            var secondary = SetupCodexNodes(1)
                            .WithStorageQuota(2.MB())
                            .BringOnline()[0];

            PerformTwoClientTest(primary, secondary);
        }

        private void PerformTwoClientTest(IOnlineCodexNode primary, IOnlineCodexNode secondary)
        {
            primary.ConnectToPeer(secondary);

            var testFile = GenerateTestFile(1.MB());

            var contentId = primary.UploadFile(testFile);

            var downloadedFile = secondary.DownloadContent(contentId);

            testFile.AssertIsEqual(downloadedFile);
        }
    }
}
