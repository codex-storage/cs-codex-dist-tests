using CodexTests;
using DistTestCore;
using NUnit.Framework;

namespace CodexLongTests.BasicTests
{
    public class TestInfraTests : CodexDistTest
    {
        [Test, UseLongTimeouts]
        public void TestInfraShouldHave1000AddressSpacesPerPod()
        {
            var group = AddCodex(1000, s => s.EnableMetrics());

            var nodeIds = group.Select(n => n.GetDebugInfo().id).ToArray();

            Assert.That(nodeIds.Length, Is.EqualTo(nodeIds.Distinct().Count()),
                "Not all created nodes provided a unique id.");
        }

        [Test, UseLongTimeouts]
        public void TestInfraSupportsManyConcurrentPods()
        {
            for (var i = 0; i < 20; i++)
            {
                var n = AddCodex();

                Assert.That(!string.IsNullOrEmpty(n.GetDebugInfo().id));
            }
        }
    }
}
