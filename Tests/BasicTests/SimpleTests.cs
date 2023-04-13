using DistTestCore;
using KubernetesWorkflow;
using NUnit.Framework;

namespace Tests.BasicTests
{
    [TestFixture]
    public class SimpleTests : DistTest
    {
        [Test]
        public void OneClientTest()
        {
            var primary = SetupCodexNodes(1).BringOnline()[0];

            PerformOneClientTest(primary);
        }

        [Test]
        public void RestartTest()
        {
            var group = SetupCodexNodes(1).BringOnline();

            var setup = group.BringOffline();

            var primary = setup.BringOnline()[0];

            PerformOneClientTest(primary);
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

        //[Test]
        //public void TwoMetricsExample()
        //{
        //    var group = SetupCodexNodes(2)
        //                .EnableMetrics()
        //                .BringOnline();

        //    var group2 = SetupCodexNodes(2)
        //                .EnableMetrics()
        //                .BringOnline();

        //    var primary = group[0];
        //    var secondary = group[1];
        //    var primary2 = group2[0];
        //    var secondary2 = group2[1];

        //    primary.ConnectToPeer(secondary);
        //    primary2.ConnectToPeer(secondary2);

        //    Thread.Sleep(TimeSpan.FromMinutes(5));

        //    primary.Metrics.AssertThat("libp2p_peers", Is.EqualTo(1));
        //    primary2.Metrics.AssertThat("libp2p_peers", Is.EqualTo(1));
        //}

        //[Test]
        //public void MarketplaceExample()
        //{
        //    var group = SetupCodexNodes(4)
        //                    .WithStorageQuota(10.GB())
        //                    .EnableMarketplace(initialBalance: 20)
        //                    .BringOnline();

        //    foreach (var node in group)
        //    {
        //        Assert.That(node.Marketplace.GetBalance(), Is.EqualTo(20));
        //    }

        //    // WIP: Balance is now only ETH.
        //    // todo: All nodes should have plenty of ETH to pay for transactions.
        //    // todo: Upload our own token, use this exclusively. ETH should be invisibile to the tests.


        //    //var secondary = SetupCodexNodes(1)
        //    //                .EnableMarketplace(initialBalance: 1000)
        //    //                .BringOnline()[0];

        //    //primary.ConnectToPeer(secondary);
        //    //primary.Marketplace.MakeStorageAvailable(10.GB(), minPricePerBytePerSecond: 1, maxCollateral: 20);

        //    //var testFile = GenerateTestFile(10.MB());
        //    //var contentId = secondary.UploadFile(testFile);
        //    //secondary.Marketplace.RequestStorage(contentId, pricePerBytePerSecond: 2,
        //    //    requiredCollateral: 10, minRequiredNumberOfNodes: 1);

        //    //primary.Marketplace.AssertThatBalance(Is.LessThan(20), "Collateral was not placed.");
        //    //var primaryBalance = primary.Marketplace.GetBalance();

        //    //secondary.Marketplace.AssertThatBalance(Is.LessThan(1000), "Contractor was not charged for storage.");
        //    //primary.Marketplace.AssertThatBalance(Is.GreaterThan(primaryBalance), "Storer was not paid for storage.");
        //}

        private void PerformOneClientTest(IOnlineCodexNode primary)
        {
            var testFile = GenerateTestFile(1.MB());

            var contentId = primary.UploadFile(testFile);

            var downloadedFile = primary.DownloadContent(contentId);

            testFile.AssertIsEqual(downloadedFile);
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
