using CodexClient;
using FileUtils;
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
        private TrackedFile file = null!;

        [TestMoment(t: Zero)]
        public void UploadTestFile()
        {
            var filesize = 80.MB();

            file = FileManager.GenerateFile(filesize);

            cid = Nodes[0].UploadFile(file);
            Assert.That(cid, Is.Not.Null);

            var dl = Nodes[0].DownloadContent(cid);
            file.AssertIsEqual(dl);
        }
    }
}
