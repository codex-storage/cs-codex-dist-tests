using CodexDistTests.TestCore;
using NUnit.Framework;

namespace CodexDistTests.BasicTests
{
    [TestFixture]
    public class DebugEndpointTests : DistTest
    {
        [Test]
        public void GetDebugInfo()
        {
            CreateCodexNode();

            var node = GetCodexNode();
            var debugInfo = node.GetDebugInfo();

            Assert.That(debugInfo.spr, Is.Not.Empty);

            DestroyCodexNode();
        }
    }
}
