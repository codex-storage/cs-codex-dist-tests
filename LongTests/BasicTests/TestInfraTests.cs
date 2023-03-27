using CodexDistTestCore;
using NUnit.Framework;

namespace LongTests.BasicTests
{
    public class TestInfraTests : DistTest
    {
        [Test, UseLongTimeouts]
        public void TestInfraShouldHave1000AddressSpacesPerPod()
        {
            var group = SetupCodexNodes(1000)
                            .EnableMetrics() // Increases use of port address space per node.
                            .BringOnline();

            var nodeIds = group.Select(n => n.GetDebugInfo().id).ToArray();

            Assert.That(nodeIds.Length, Is.EqualTo(nodeIds.Distinct().Count()),
                "Not all created nodes provided a unique id.");
        }

        [Test, UseLongTimeouts]
        public void TestInfraSupportsManyConcurrentPods()
        {
            for (var i = 0; i < 20; i++)
            {
                var n = SetupCodexNodes(1).BringOnline()[0];

                Assert.That(!string.IsNullOrEmpty(n.GetDebugInfo().id));
            }
        }

        [Test, UseLongTimeouts]
        public void DownloadConsistencyTest()
        {
            var primary = SetupCodexNodes(1)
                            .WithLogLevel(CodexLogLevel.Trace)
                            .WithStorageQuota(2.MB())
                            .BringOnline()[0];

            var testFile = GenerateTestFile(1.MB());

            var contentId = primary.UploadFile(testFile);

            var files = new List<TestFile?>();
            for (var i = 0; i < 100; i++)
            {
                files.Add(primary.DownloadContent(contentId));
            }

            Assert.That(files.All(f => f != null));
            Assert.That(files.All(f => f!.GetFileSize() == testFile.GetFileSize()));
            foreach (var file in files) file!.AssertIsEqual(testFile);
        }
    }
}
