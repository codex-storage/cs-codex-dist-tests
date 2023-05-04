using DistTestCore;
using NUnit.Framework;
using Utils;

namespace Tests.BasicTests
{
    [Ignore("not a real test!")]
    [TestFixture]
    public class NetworkIsolationTest : DistTest
    {
        private IOnlineCodexNode? node = null;

        // net isolation: only on real cluster?
        // parallel upload/download tests?
        // operation times.

        [Test]
        public void SetUpANodeAndWait()
        {
            node = SetupCodexNode();

            while (node != null)
            {
                Time.Sleep(TimeSpan.FromSeconds(5));
            }
        }

        [Test]
        public void ForeignNodeConnects()
        {
            var myNode = SetupCodexNode();

            while (node == null)
            {
                Time.Sleep(TimeSpan.FromSeconds(1));
            }

            myNode.ConnectToPeer(node);

            var testFile = GenerateTestFile(1.MB());

            var contentId = node.UploadFile(testFile);

            var downloadedFile = myNode.DownloadContent(contentId);

            testFile.AssertIsEqual(downloadedFile);

            node = null;
        }
    }
}
