using CodexPlugin;
using DistTestCore;
using NUnit.Framework;
using Utils;

namespace Tests.BasicTests
{
    // Warning!
    // This is a test to check network-isolation in the test-infrastructure.
    // It requires parallelism(2) or greater to run.
    [TestFixture]
    [Ignore("Disabled until a solution is implemented.")]
    public class NetworkIsolationTest : DistTest
    {
        private IOnlineCodexNode? node = null;

        [Test]
        public void SetUpANodeAndWait()
        {
            node = Ci.SetupCodexNode();

            Time.WaitUntil(() => node == null, TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5));
        }

        [Test]
        public void ForeignNodeConnects()
        {
            var myNode = Ci.SetupCodexNode();

            Time.WaitUntil(() => node != null, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(5));

            try
            {
                myNode.ConnectToPeer(node!);
            }
            catch
            {
                // Good! This connection should be prohibited by the network isolation policy.
                node = null;
                return;
            }

            Assert.Fail("Connection could be established between two Codex nodes running in different namespaces. " +
                "This may cause cross-test interference. Network isolation policy should be applied. Test infra failure.");
        }
    }
}
