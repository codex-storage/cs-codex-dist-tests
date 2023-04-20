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
            var group = SetupCodexNodes(2).BringOnline();

            var primary = group[0];
            var secondary = group[1];

            PerformTwoClientTest(primary, secondary);
        }

        [Test]
        public void TwoClientsTwoPodsTest()
        {
            var primary = SetupCodexNodes(1).BringOnline()[0];

            var secondary = SetupCodexNodes(1).BringOnline()[0];

            PerformTwoClientTest(primary, secondary);
        }

        [Test]
        [Ignore("Requires Location map to be configured for k8s cluster.")]
        public void TwoClientsTwoLocationsTest()
        {
            var primary = SetupCodexNodes(1)
                            .At(Location.BensLaptop)
                            .BringOnline()[0];

            var secondary = SetupCodexNodes(1)
                            .At(Location.BensOldGamingMachine)
                            .BringOnline()[0];

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
