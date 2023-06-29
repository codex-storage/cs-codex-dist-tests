using DistTestCore;
using DistTestCore.Codex;
using NUnit.Framework;

namespace ContinuousTests.Tests
{
    public class UploadPerformanceTest : PerformanceTest
    {
        public override int RequiredNumberOfNodes => 1;

        [TestMoment(t: Zero)]
        public void UploadTest()
        {
            UploadTest(100, Nodes[0]);
        }
    }

    public class DownloadLocalPerformanceTest : PerformanceTest
    {
        public override int RequiredNumberOfNodes => 1;

        [TestMoment(t: Zero)]
        public void DownloadTest()
        {
            DownloadTest(100, Nodes[0], Nodes[0]);
        }
    }

    public class DownloadRemotePerformanceTest : PerformanceTest
    {
        public override int RequiredNumberOfNodes => 2;

        [TestMoment(t: Zero)]
        public void DownloadTest()
        {
            DownloadTest(100, Nodes[0], Nodes[1]);
        }
    }

    public abstract class PerformanceTest : ContinuousTest
    {
        public override TimeSpan RunTestEvery => TimeSpan.FromHours(1);
        public override TestFailMode TestFailMode => TestFailMode.AlwaysRunAllMoments;

        public void UploadTest(int megabytes, CodexAccess uploadNode)
        {
            var file = FileManager.GenerateTestFile(megabytes.MB());

            var time = Measure(() =>
            {
                UploadFile(uploadNode, file);
            });

            var timePerMB = time / megabytes;

            Assert.That(timePerMB, Is.LessThan(CodexContainerRecipe.MaxUploadTimePerMegabyte), "MaxUploadTimePerMegabyte performance threshold breached.");
        }

        public void DownloadTest(int megabytes, CodexAccess uploadNode, CodexAccess downloadNode)
        {
            var file = FileManager.GenerateTestFile(megabytes.MB());

            var cid = UploadFile(uploadNode, file);
            Assert.That(cid, Is.Not.Null);

            TestFile? result = null;
            var time = Measure(() =>
            {
                result = DownloadFile(downloadNode, cid!);
            });

            file.AssertIsEqual(result);

            var timePerMB = time / megabytes;

            Assert.That(timePerMB, Is.LessThan(CodexContainerRecipe.MaxDownloadTimePerMegabyte), "MaxDownloadTimePerMegabyte performance threshold breached.");
        }

        private static TimeSpan Measure(Action action)
        {
            var start = DateTime.UtcNow;
            action();
            return DateTime.UtcNow - start;
        }
    }
}
