using CodexDistTestCore;
using NUnit.Framework;

namespace Tests.BasicTests
{
    [TestFixture]
    public class SimpleTests : DistTest
    {
        [Test]
        public void DoCommand()
        {
            var primary = SetupCodexNodes(1).BringOnline()[0];

            k8sManager.ExampleOfCMD(primary);
        }

        [Test]
        public void TwoMetricsExample()
        {
            var group = SetupCodexNodes(2)
                        .EnableMetrics()
                        .BringOnline();

            var group2 = SetupCodexNodes(2)
                        .EnableMetrics()
                        .BringOnline();

            var primary = group[0];
            var secondary = group[1];
            var primary2 = group2[0];
            var secondary2 = group2[1];

            primary.ConnectToPeer(secondary);
            primary2.ConnectToPeer(secondary2);

            Thread.Sleep(TimeSpan.FromMinutes(5));

            primary.Metrics.AssertThat("libp2p_peers", Is.EqualTo(1));
            primary2.Metrics.AssertThat("libp2p_peers", Is.EqualTo(1));
        }

        [Test]
        public void OneClientTest()
        {
            var primary = SetupCodexNodes(1).BringOnline()[0];

            var testFile = GenerateTestFile(1.MB());

            var contentId = primary.UploadFile(testFile);

            var downloadedFile = primary.DownloadContent(contentId);

            testFile.AssertIsEqual(downloadedFile);
        }

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
