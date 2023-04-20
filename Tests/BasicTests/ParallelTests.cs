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
            ParallelDownload(3, 64.MB());
        }
        [Test]
        public void FourNodeDownloads()
        {
            ParallelDownload(5, 1000.MB());
        }
        [Test]
        public void NineNodeDownloads()
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
                // new Task(() => { download(contentId, group[i], testFile); }).Start();
                download(contentId, group[i], testFile);
            }
            // Task.WaitAll();
        }
    }

    [TestFixture]
    public class UploadTests : DistTest
    {
        [Test]
        public void ThreeNodeUploads()
        {
            ParallelUpload(3, 64.MB());
        }
        [Test]
        public void FiveNodeUploads()
        {
            ParallelUpload(5, 1000.MB());
        }
        [Test]
        public void TenNodeUploads()
        {
            ParallelUpload(10, 16.MB());
        }
        void ParallelUpload(int numberOfNodes, ByteSize filesize)
        {
            var group = SetupCodexNodes(numberOfNodes).EnableMetrics().BringOnline();

            var host = group[0];

            for (int i = 1; i < numberOfNodes; i++)
            {
                host.ConnectToPeer(group[i]);
            }
            var testfiles = new List<TestFile>();
            var contentIds = new List<ContentId>();
            for (int i = 1; i < numberOfNodes; i++)
            {
                testfiles.Add(GenerateTestFile(filesize));
                // new Task(() => { upload(host, testfiles[i - 1], contentIds, i - 1); }).Start();
                upload(host, testfiles[i - 1], contentIds, i - 1);
            }
            // Task.WaitAll();
            for (int i = 0; i < testfiles.Count; i++)
            {
                // new Task(() => { download(contentIds[i], group[i + 1], testfiles[i]); }).Start();
                download(contentIds[i], group[i + 1], testfiles[i]);
            }
            // Task.WaitAll();
        }

        void download(ContentId contentId, CodexDistTestCore.IOnlineCodexNode node, TestFile testFile)
        {
            var downloadedFile = node.DownloadContent(contentId);
            testFile.AssertIsEqual(downloadedFile);
        }
        void upload(CodexDistTestCore.IOnlineCodexNode host, TestFile testfile, List<ContentId> contentIds, int pos)
        {
            contentIds[pos] = host.UploadFile(testfile);
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