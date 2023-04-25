using DistTestCore;
using KubernetesWorkflow;
using NUnit.Framework;

namespace Tests.BasicTests
{
    [TestFixture]
    public class TwoClientTests : DistTest
    {
        [Test]
        public void TwoClientsOnePodTest()
        {
            var group = SetupCodexNodes(2);

            var primary = group[0];
            var secondary = group[1];

            PerformTwoClientTest(primary, secondary);
        }

        [Test]
        public void TwoClientsTwoPodsTest()
        {
            var primary = SetupCodexNode();
            var secondary = SetupCodexNode();

            PerformTwoClientTest(primary, secondary);
        }

        [Test]
        [Ignore("Requires Location map to be configured for k8s cluster.")]
        public void TwoClientsTwoLocationsTest()
        {
            var primary = SetupCodexNode(s => s.At(Location.BensLaptop));
            var secondary = SetupCodexNode(s => s.At(Location.BensOldGamingMachine));

            PerformTwoClientTest(primary, secondary);
        }

        private void PerformTwoClientTest(IOnlineCodexNode primary, IOnlineCodexNode secondary)
        {
            primary.ConnectToPeer(secondary);

            var testFile = GenerateTestFile(1.MB());

            var contentId = primary.UploadFile(testFile);

            var downloadedFile = secondary.DownloadContent(contentId);

            testFile.AssertIsEqual(downloadedFile);
        }
    }
}
