using DistTestCore;
using NUnit.Framework;

namespace Tests.ParallelTests
{
    [TestFixture]
    public class MixedTests : DistTest
    {
        [TestCase(1, 10)]
        [UseLongTimeouts]
        public void ParallelMixed(int numberOfNodes, int filesizeMb)
        {
            // initialize the nodes
            var group = SetupCodexNodes(numberOfNodes);
            var host = SetupCodexNode();

            foreach (var node in group)
            {
                host.ConnectToPeer(node);
            }
            // Upload single file for the download nodes
            var testfile = GenerateTestFile(filesizeMb.MB());
            var contentId = host.UploadFile(testfile);

            var testfiles = new List<TestFile>();
            var contentIds = new List<Task<ContentId>>();

            // Starts uploads for the upload nodes
            for (int i = 0; i < group.Count(); i++)
            {
                testfiles.Add(GenerateTestFile(filesizeMb.MB()));
                var n = i;
                contentIds.Add(Task.Run(() => { return host.UploadFile(testfiles[n]); }));
            }
            // Starts downloads for the download nodes
            var downloads = new List<Task<TestFile?>>();
            for (int i = 0; i < group.Count(); i++)
            {
                var n = i;
                downloads.Add(Task.Run(() => { return group[n].DownloadContent(contentId); }));
            }
            Task.WaitAll(downloads.ToArray());
            for (int i = 0; i < group.Count(); i++)
            {
                testfiles[i].AssertIsEqual(downloads[i].Result);
            }
        }
    }
}
