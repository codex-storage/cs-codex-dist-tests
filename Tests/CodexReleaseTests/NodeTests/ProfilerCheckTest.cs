using CodexTests;
using DistTestCore;
using NUnit.Framework;

namespace CodexReleaseTests.NodeTests
{
    [TestFixture]
    public class ProfilerCheckTest : CodexDistTest
    {
        [Test]
        public void IsProfilingImage()
        {
            var node = StartCodex();
            var log = node.DownloadLog();
            node.Stop(waitTillStopped: false);

            log.AssertLogContains("Enabling profiling");
        }
    }
}
