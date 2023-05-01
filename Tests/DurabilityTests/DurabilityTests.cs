using DistTestCore;
using DistTestCore.Codex;
using NUnit.Framework;
using Utils;

namespace Tests.DurabilityTests
{
    [TestFixture]
    public class DurabilityTests : DistTest
    {
        [Test]
        public void BootstrapNodeDisappearsTest()
        {
            var bootstrapNode = SetupCodexNode();
            var group = SetupCodexNodes(2, s => s.WithBootstrapNode(bootstrapNode));
            var primary = group[0];
            var secondary = group[1];

            // There is 1 minute of time for the nodes to connect to each other.
            // (Should be easy, they're in the same pod.)
            Time.Sleep(TimeSpan.FromMinutes(6));
            bootstrapNode.BringOffline();

            var file = GenerateTestFile(10.MB());
            var contentId = primary.UploadFile(file);
            var downloadedFile = secondary.DownloadContent(contentId);

            file.AssertIsEqual(downloadedFile);
        }

        [Test]
        public void DataRetentionTest()
        {
            var bootstrapNode = SetupCodexNode(s => s.WithLogLevel(CodexLogLevel.Trace));

            var startGroup = SetupCodexNodes(2, s => s.WithLogLevel(CodexLogLevel.Trace).WithBootstrapNode(bootstrapNode));
            var finishGroup = SetupCodexNodes(10, s => s.WithLogLevel(CodexLogLevel.Trace).WithBootstrapNode(bootstrapNode));

            var file = GenerateTestFile(10.MB());

            // Both nodes in the start group have the file.
            var content = startGroup[0].UploadFile(file);
            DownloadAndAssert(content, file, startGroup[1]);

            // Three nodes of the finish group have the file.
            DownloadAndAssert(content, file, finishGroup[0]);
            DownloadAndAssert(content, file, finishGroup[1]);
            DownloadAndAssert(content, file, finishGroup[2]);

            // The start group goes away.
            startGroup.BringOffline();

            // All nodes in the finish group can access the file.
            foreach (var node in finishGroup)
            {
                DownloadAndAssert(content, file, node);
            }
        }

        private void DownloadAndAssert(ContentId content, TestFile file, IOnlineCodexNode onlineCodexNode)
        {
            var downloaded = onlineCodexNode.DownloadContent(content);
            file.AssertIsEqual(downloaded);
        }
    }
}
