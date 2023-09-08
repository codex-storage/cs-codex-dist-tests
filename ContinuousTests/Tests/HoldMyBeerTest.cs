using DistTestCore;
using NUnit.Framework;
using Utils;

namespace ContinuousTests.Tests
{
    public class HoldMyBeerTest : ContinuousTest
    {
        public override int RequiredNumberOfNodes => 1;
        public override TimeSpan RunTestEvery => TimeSpan.FromMinutes(2);
        public override TestFailMode TestFailMode => TestFailMode.StopAfterFirstFailure;

        private ContentId? cid;
        private TestFile file = null!;

        [TestMoment(t: Zero)]
        public void UploadTestFile()
        {
            var filesize = 80.MB();

            file = FileManager.GenerateTestFile(filesize);

            cid = UploadFile(Nodes[0], file);
            Assert.That(cid, Is.Not.Null);

            var dl = DownloadFile(Nodes[0], cid!);
            file.AssertIsEqual(dl);
        }
    }
}
