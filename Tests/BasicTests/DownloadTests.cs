using DistTestCore;
using KubernetesWorkflow;
using NUnit.Framework;
namespace Tests.ParallelTests
{
    [TestFixture]
    public class DownloadTests : DistTest
    {
        [Test]
        public void ThreeNodeDownloads()
        {
            ParallelDownload(3, 5000.MB());
        }
        [Test]
        public void FiveNodeDownloads()
        {
            ParallelDownload(5, 1000.MB());
        }
        [Test]
        public void TenNodeDownloads()
        {
            ParallelDownload(10, 256.MB());
        }

        void ParallelDownload(int numberOfNodes, ByteSize filesize)
        {
            var group = SetupCodexNodes(numberOfNodes);
            var host = SetupCodexNode();

            foreach (var node in group)
            {
                host.ConnectToPeer(node);
            }

            var testFile = GenerateTestFile(filesize);
            var contentId = host.UploadFile(testFile);
            var list = new List<Task<TestFile?>>();
            
            foreach (var node in group)
            {
                list.Add(Task.Run(() => { return node.DownloadContent(contentId); }));
            }

            Task.WaitAll(list.ToArray());
            foreach (var task in list)
            {
                testFile.AssertIsEqual(task.Result);
            }
        }
    }
}