using CodexPlugin;
using CodexTests;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                if (!ex.Message.StartsWith("Retry 'DownloadFile' timed out"))
                {
                    throw;
                }
            }

            WaitAndCheckNodesStaysAlive(TimeSpan.FromMinutes(2), node);
        }
    }
}
