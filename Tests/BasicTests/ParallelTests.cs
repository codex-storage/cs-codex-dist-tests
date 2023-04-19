using CodexDistTestCore;
using CodexDistTestCore.Config;
using NUnit.Framework;

namespace Tests.ParallelTests
{
    [TestFixture]
    public class DownloadTests : DistTest
    {
        [Test]
        public void TwoNodeDownloads()
        {
            ParallelDownload(2, 64.MB());
        }
        [Test]
        public void FiveNodeDownloads()
        {
            ParallelDownload(5, 1000.MB());
        }
        [Test]
        public void TenNodeDownloads()
        {
            ParallelDownload(10, 16.MB());
        }
        public void download(ContentId contentId, CodexDistTestCore.IOnlineCodexNode node, TestFile testFile)
        {
            var downloadedFile = node.DownloadContent(contentId);
            testFile.AssertIsEqual(downloadedFile);
        }

        void ParallelDownload(int numberOfNodes, ByteSize filesize)
        {
            var group = SetupCodexNodes(numberOfNodes).EnableMetrics().BringOnline();

            var host = group[0];

            for (int i = 1; i < numberOfNodes; i++)
            {
                host.ConnectToPeer(group[i]);
            }

            var testFile = GenerateTestFile(filesize);

            var contentId = host.UploadFile(testFile);

            for (int i = 1; i < numberOfNodes; i++)
            {
                new Task(() => { download(contentId, group[i], testFile); }).Start();
            }
            Task.WaitAll();
        }
    }

    [TestFixture]
    public class UploadTests : DistTest
    {
        [Test]
        public void TwoNodeUploads()
        {
        }

        public void FiveNodeUploads()
        {
        }
        public void TenNodeUploads()
        {
        }
    }
    [TestFixture]
    public class MixedTests : DistTest
    {
        [Test]
        public void OneDownloadOneUpload()
        {
        }

        public void ThreeDownloadTwoUpload()
        {
        }
        public void FiveDownloadFiveUpload()
        {
        }
    }
}