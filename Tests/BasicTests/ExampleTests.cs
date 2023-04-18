using DistTestCore;
using DistTestCore.Codex;
using NUnit.Framework;
using Utils;

namespace Tests.BasicTests
{
    [TestFixture]
    public class ExampleTests : DistTest
    {
        [Test]
        public void CodexLogExample()
        {
            var primary = SetupCodexNodes(1)
                            .WithLogLevel(CodexLogLevel.Trace)
                            .BringOnline()[0];

            primary.UploadFile(GenerateTestFile(5.MB()));

            var log = primary.DownloadLog();

            log.AssertLogContains("Uploaded file");
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
        public void MarketplaceExample()
        {
            var primary = SetupCodexNodes(1)
                            .WithStorageQuota(10.GB())
                            .EnableMarketplace(initialBalance: 234.TestTokens())
                            .BringOnline()[0];

            Assert.That(primary.Marketplace.GetBalance(), Is.EqualTo(234));

            var secondary = SetupCodexNodes(1)
                            .EnableMarketplace(initialBalance: 1000.TestTokens())
                            .BringOnline()[0];

            primary.ConnectToPeer(secondary);

            // Gives 503 - Persistance not enabled in current codex image.
            primary.Marketplace.MakeStorageAvailable(
                size: 10.GB(),
                minPricePerBytePerSecond: 1.TestTokens(),
                maxCollateral: 20.TestTokens(),
                maxDuration: TimeSpan.FromMinutes(3));

            var testFile = GenerateTestFile(10.MB());
            var contentId = secondary.UploadFile(testFile);

            // Gives 500 - Persistance not enabled in current codex image.
            secondary.Marketplace.RequestStorage(contentId,
                pricePerBytePerSecond: 2.TestTokens(),
                requiredCollateral: 10.TestTokens(),
                minRequiredNumberOfNodes: 1,
                proofProbability: 5,
                duration: TimeSpan.FromMinutes(2));

            Time.Sleep(TimeSpan.FromMinutes(3));

            primary.Marketplace.AssertThatBalance(Is.LessThan(20), "Collateral was not placed.");
            var primaryBalance = primary.Marketplace.GetBalance();

            secondary.Marketplace.AssertThatBalance(Is.LessThan(1000), "Contractor was not charged for storage.");
            primary.Marketplace.AssertThatBalance(Is.GreaterThan(primaryBalance), "Storer was not paid for storage.");
        }
    }
}
