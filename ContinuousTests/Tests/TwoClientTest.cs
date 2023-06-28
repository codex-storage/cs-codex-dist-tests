using DistTestCore;
using NUnit.Framework;

namespace ContinuousTests.Tests
{
    public class TwoClientTest : ContinuousTest
    {
        public override int RequiredNumberOfNodes => 2;
        public override TimeSpan RunTestEvery => TimeSpan.FromHours(1);
        public override TestFailMode TestFailMode => TestFailMode.StopAfterFirstFailure;

        private ContentId? cid;
        private TestFile file = null!;

        [TestMoment(t: Zero)]
        public void UploadTestFile()
        {
            file = FileManager.GenerateTestFile(10.MB());

            cid = UploadFile(Nodes[0], file);
            Assert.That(cid, Is.Not.Null);
        }

        [TestMoment(t: MinuteFive)]
        public void DownloadTestFile()
        {
            var dl = DownloadFile(Nodes[1], cid!);

            file.AssertIsEqual(dl);
        }
    }
}
