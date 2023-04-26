using DistTestCore;
using KubernetesWorkflow;
using NUnit.Framework;
namespace Tests.ParallelTests
{
    [TestFixture]
    public class UploadTests : DistTest
    {
        [Test]
        public void ThreeNodeUploads()
        {
            ParallelUpload(3, 50.MB());
        }
        [Test]
        public void FiveNodeUploads()
        {
            ParallelUpload(5, 750.MB());
        }
        [Test]
        public void TenNodeUploads()
        {
            ParallelUpload(10, 25.MB());
        }
        void ParallelUpload(int numberOfNodes, ByteSize filesize)
        {
            var group = SetupCodexNodes(numberOfNodes).BringOnline();
            var host = SetupCodexNodes(1).BringOnline()[0];

            foreach (var node in group)
            {
                host.ConnectToPeer(node);
            }

            var testfiles = new List<TestFile>();
            var contentIds = new List<Task<ContentId>>();

            for (int i = 0; i < group.Count(); i++)
            {
                testfiles.Add(GenerateTestFile(filesize));
                var n = i;
                contentIds.Add(Task.Run(() => { return host.UploadFile(testfiles[n]); }));
            }
            var downloads = new List<Task<TestFile?>>();
            for (int i = 0; i < group.Count(); i++)
            {
                var n = i;
                downloads.Add(Task.Run(() => { return group[n].DownloadContent(contentIds[n].Result); }));
            }
            Task.WaitAll(downloads.ToArray());
            for (int i = 0; i < group.Count(); i++)
            {
                testfiles[i].AssertIsEqual(downloads[i].Result);
            }
        }
    }
}