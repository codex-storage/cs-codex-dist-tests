using CodexPlugin;
using FileUtils;
using NUnit.Framework;
using Utils;

namespace ContinuousTests.Tests
{
    public class TwoClientTest : ContinuousTest
    {
        public override int RequiredNumberOfNodes => 2;
        public override TimeSpan RunTestEvery => TimeSpan.FromMinutes(2);
        public override TestFailMode TestFailMode => TestFailMode.StopAfterFirstFailure;

        private ContentId? cid;
        private TrackedFile file = null!;

        [TestMoment(t: Zero)]
        public void UploadTestFile()
        {
            file = FileManager.GenerateFile(80.MB());

            cid = Nodes[0].UploadFile(file);
            Assert.That(cid, Is.Not.Null);
        }

        [TestMoment(t: 10)]
        public void DownloadTestFile()
        {
            var dl = Nodes[1].DownloadContent(cid!);

            file.AssertIsEqual(dl);
        }
    }
}
