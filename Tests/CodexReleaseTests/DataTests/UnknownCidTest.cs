using CodexClient;
using CodexTests;
using NUnit.Framework;

namespace CodexReleaseTests.DataTests
{
    [TestFixture]
    public class UnknownCidTest : CodexDistTest
    {
        [Test]
        public void DownloadingUnknownCidDoesNotCauseCrash()
        {
            var node = StartCodex();

            var unknownCid = new ContentId("zDvZRwzkzHsok3Z8yMoiXE9EDBFwgr8WygB8s4ddcLzzSwwXAxLZ");

            var localFiles = node.LocalFiles().Content;
            CollectionAssert.DoesNotContain(localFiles.Select(f => f.Cid), unknownCid);

            try
            {
                node.DownloadContent(unknownCid);
            }
            catch (Exception ex)
            {
                var expectedMessage = $"Download of '{unknownCid.Id}' timed out";
                if (!ex.Message.StartsWith(expectedMessage)) throw;
            }

            WaitAndCheckNodesStaysAlive(TimeSpan.FromMinutes(2), node);
        }
    }
}
