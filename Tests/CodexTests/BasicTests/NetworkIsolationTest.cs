using CodexPlugin;
using DistTestCore;
using NUnit.Framework;
using Utils;

namespace CodexTests.BasicTests
{
    // Warning!
    // This is a test to check network-isolation in the test-infrastructure.
    // It requires parallelism(2) or greater to run.
    [TestFixture]
    [Ignore("Disabled until a solution is implemented.")]
    public class NetworkIsolationTest : DistTest
    {
        private ICodexNode? node = null;

        [Test]
        public void SetUpANodeAndWait()
        {
            node = Ci.StartCodexNode();

            Time.WaitUntil(() => node == null, TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5), nameof(SetUpANodeAndWait));
        }

        [Test]
        public void ForeignNodeConnects()
        {
            var myNode = Ci.StartCodexNode();

            Time.WaitUntil(() => node != null, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(5), nameof(ForeignNodeConnects));

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
