using CodexPlugin;
using CodexTests;
using DistTestCore;
using FileUtils;
using NUnit.Framework;
using Utils;

namespace CodexLongTests.BasicTests
{
    [TestFixture]
    public class UploadTests : CodexDistTest
    {
        [TestCase(3, 50)]
        [TestCase(5, 75)]
        [TestCase(10, 25)]
        [UseLongTimeouts]
        public void ParallelUpload(int numberOfNodes, int filesizeMb)
        {
            var group = AddCodex(numberOfNodes);
            var host = AddCodex();

            foreach (var node in group)
            {
                host.ConnectToPeer(node);
            }

            var testfiles = new List<TrackedFile>();
            var contentIds = new List<Task<ContentId>>();

            for (int i = 0; i < group.Count(); i++)
            {
                testfiles.Add(GenerateTestFile(filesizeMb.MB()));
                var n = i;
                contentIds.Add(Task.Run(() => { return host.UploadFile(testfiles[n]); }));
            }
            var downloads = new List<Task<TrackedFile?>>();
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
