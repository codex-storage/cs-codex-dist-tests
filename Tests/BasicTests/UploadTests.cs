using DistTestCore;
using NUnit.Framework;

namespace Tests.ParallelTests
{
    [TestFixture]
    public class UploadTests : DistTest
    {
        [TestCase(3, 50)]
        [TestCase(5, 75)]
        [TestCase(10, 25)]
        [UseLongTimeouts]
        public void ParallelUpload(int numberOfNodes, int filesizeMb)
        {
            var group = SetupCodexNodes(numberOfNodes);
            var host = SetupCodexNode();

            foreach (var node in group)
            {
                host.ConnectToPeer(node);
            }

            var testfiles = new List<TestFile>();
            var contentIds = new List<Task<ContentId>>();

            for (int i = 0; i < group.Count(); i++)
            {
                testfiles.Add(GenerateTestFile(filesizeMb.MB()));
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
