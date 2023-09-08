using DistTestCore;
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
        private TestFile file = null!;

        [TestMoment(t: Zero)]
        public void UploadTestFile()
        {
            file = FileManager.GenerateTestFile(80.MB());

            cid = UploadFile(Nodes[0], file);
            Assert.That(cid, Is.Not.Null);
        }

        [TestMoment(t: 10)]
        public void DownloadTestFile()
        {
            var dl = DownloadFile(Nodes[1], cid!);

            file.AssertIsEqual(dl);
        }
    }
}
