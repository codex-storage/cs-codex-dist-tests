using NUnit.Framework;
using WakuPlugin;

namespace WakuTests
{
    public class BasicTests : WakuDistTest
    {
        [Test]
        public void Hi()
        {
            var bootNode = Ci.StartWakuNode(s => s.WithName("BootstrapNode"));
            var node = Ci.StartWakuNode(s => s.WithName("Waku1").WithBootstrapNode(bootNode));

            var topic = "cheeseWheels";
            var message = "hmm, cheese...";

            bootNode.SubscribeToTopic(topic);
            node.SubscribeToTopic(topic);

            node.SendMessage(topic, message);

            var received = bootNode.GetMessages(topic);

            CollectionAssert.Contains(received, message);
        }
    }
}