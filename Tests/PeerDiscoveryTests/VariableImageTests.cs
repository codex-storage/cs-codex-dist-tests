using DistTestCore;
using DistTestCore.Codex;
using NUnit.Framework;

namespace Tests.PeerDiscoveryTests
{
    [TestFixture]
    public class VariableImageTests : DistTest
    {
        [TestCase("nim-codex:sha-a899384")]
        [TestCase("nim-codex:sha-3879ec8")]
        [TestCase("nim-codex:sha-6dd7e55")]
        [TestCase("nim-codex:sha-3f2b417")]
        [TestCase("nim-codex:sha-00f6554")]
        [TestCase("nim-codex:sha-f053135")]
        [TestCase("nim-codex:sha-711e5e0")]
        public void ThreeNodes(string dockerImage)
        {
            var img = "codexstorage/" + dockerImage;
            Log("Image override: " + img);
            CodexContainerRecipe.DockerImageOverride = img;

            var boot = SetupCodexBootstrapNode();
            SetupCodexNode(c => c.WithBootstrapNode(boot));
            SetupCodexNode(c => c.WithBootstrapNode(boot));

            PeerConnectionTestHelpers.AssertFullyConnected(GetAllOnlineCodexNodes());
        }
    }
}

