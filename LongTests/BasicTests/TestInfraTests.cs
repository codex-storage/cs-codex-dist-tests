﻿using DistTestCore;
using NUnit.Framework;

namespace TestsLong.BasicTests
{
    public class TestInfraTests : DistTest
    {
        [Test, UseLongTimeouts]
        public void TestInfraShouldHave1000AddressSpacesPerPod()
        {
            var group = SetupCodexNodes(1000, s => s.EnableMetrics()); // Increases use of port address space per node.

            var nodeIds = group.Select(n => n.GetDebugInfo().id).ToArray();

            Assert.That(nodeIds.Length, Is.EqualTo(nodeIds.Distinct().Count()),
                "Not all created nodes provided a unique id.");
        }

        [Test, UseLongTimeouts]
        public void TestInfraSupportsManyConcurrentPods()
        {
            for (var i = 0; i < 20; i++)
            {
                var n = SetupCodexNode();

                Assert.That(!string.IsNullOrEmpty(n.GetDebugInfo().id));
            }
        }
    }
}
