using CodexDistTestCore;
using CodexDistTestCore.Config;
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

        [Test, DontDownloadLogsOnFailure]
        public void CanAccessLogs()
        {
            var node = SetupCodexNodes(1).BringOnline()[0];

            var log = node.DownloadLog();

            log.AssertLogContains("Started codex node");
        }

        [Test]
        public void OneClientTest()
        {
            var primary = SetupCodexNodes(1).BringOnline()[0];

            var testFile = GenerateTestFile(1.MB());

            var contentId = primary.UploadFile(testFile);

            var downloadedFile = primary.DownloadContent(contentId);

            testFile.AssertIsEqual(downloadedFile);
        }

        [Test]
        public void TwoClientsOnePodTest()
        {
            var group = SetupCodexNodes(2).BringOnline();

            var primary = group[0];
            var secondary = group[1];

            PerformTwoClientTest(primary, secondary);
        }

        [Test]
        public void TwoClientsTwoPodsTest()
        {
            var primary = SetupCodexNodes(1).BringOnline()[0];

            var secondary = SetupCodexNodes(1).BringOnline()[0];

            PerformTwoClientTest(primary, secondary);
        }

        [Test]
        public void TwoClientsTwoLocationsTest()
        {
            var primary = SetupCodexNodes(1)
                            .At(Location.BensLaptop)
                            .BringOnline()[0];

            var secondary = SetupCodexNodes(1)
                            .At(Location.BensOldGamingMachine)
                            .BringOnline()[0];

            var debugInfo = primary.GetDebugInfo();
            Assert.That(debugInfo.spr, Is.Not.Empty);

            var debugInfo2 = secondary.GetDebugInfo();
            Assert.That(debugInfo2.spr, Is.Not.Empty);

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
