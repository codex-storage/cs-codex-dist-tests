using NUnit.Framework;
using WakuPlugin;

namespace WakuTests
{
    public class BasicTests : WakuDistTest
    {
        [Test]
        public void Hi()
        {
            var node1 = Ci.StartWakuNode();

            var info1 = node1.DebugInfo();
            Assert.That(info1.enrUri, Is.Not.Empty);

            var node2 = Ci.StartWakuNode(s => s.WithBootstrapNode(node1));
            var info2 = node2.DebugInfo();
            Assert.That(info2.enrUri, Is.Not.Empty);


        }
    }
}