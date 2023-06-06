using DistTestCore;
using NUnit.Framework;

namespace TestsLong.BasicTests
{
    [TestFixture]
    public class DownloadTests : DistTest
    {
        [Test]
        [Combinatorial]
        [UseLongTimeouts]
        public void DownloadCorrectnessTest(
            [Values(1, 10, 100, 1024)] int sizeInMB,
            [Values(1, 10, 100, 1024)] int multiplier)
        {
            var size = (sizeInMB * multiplier).MB();

            var expectedFile = GenerateTestFile(size);

            var node = SetupCodexNode();
            var cid = node.UploadFile(expectedFile);
            var actualFile = node.DownloadContent(cid);

            expectedFile.AssertIsEqual(actualFile);
        }


        [TestCase(3, 500)]
        [TestCase(5, 100)]
        [TestCase(10, 256)]
        [UseLongTimeouts]
        public void ParallelDownload(int numberOfNodes, int filesizeMb)
        {
            var group = SetupCodexNodes(numberOfNodes);
            var host = SetupCodexNode();

            foreach (var node in group)
            {
                host.ConnectToPeer(node);
            }

            var testFile = GenerateTestFile(filesizeMb.MB());
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
