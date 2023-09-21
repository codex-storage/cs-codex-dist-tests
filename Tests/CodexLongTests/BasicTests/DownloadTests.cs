using DistTestCore;
using FileUtils;
using NUnit.Framework;
using Tests;
using Utils;

namespace CodexLongTests.BasicTests
{
    [TestFixture]
    public class DownloadTests : CodexDistTest
    {
        [TestCase(3, 500)]
        [TestCase(5, 100)]
        [TestCase(10, 256)]
        [UseLongTimeouts]
        public void ParallelDownload(int numberOfNodes, int filesizeMb)
        {
            var group = AddCodex(numberOfNodes);
            var host = AddCodex();

            foreach (var node in group)
            {
                host.ConnectToPeer(node);
            }

            var testFile = GenerateTestFile(filesizeMb.MB());
            var contentId = host.UploadFile(testFile);
            var list = new List<Task<TrackedFile?>>();

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
