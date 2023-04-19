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

            Thread.Sleep(TimeSpan.FromMinutes(2));

            primary.Metrics.AssertThat("libp2p_peers", Is.EqualTo(1));
            primary2.Metrics.AssertThat("libp2p_peers", Is.EqualTo(1));
        }

        [Test]
        public void MarketplaceExample()
        {
            var primary = SetupCodexNodes(1)
                            .WithStorageQuota(11.GB())
                            .EnableMarketplace(initialBalance: 234.TestTokens())
                            .BringOnline()[0];

            primary.Marketplace.AssertThatBalance(Is.EqualTo(234.TestTokens()));

            var secondary = SetupCodexNodes(1)
                            .EnableMarketplace(initialBalance: 1000.TestTokens())
                            .BringOnline()[0];

            primary.ConnectToPeer(secondary);

            primary.Marketplace.MakeStorageAvailable(
                size: 10.GB(),
                minPricePerBytePerSecond: 1.TestTokens(),
                maxCollateral: 20.TestTokens(),
                maxDuration: TimeSpan.FromMinutes(3));

            var testFile = GenerateTestFile(10.MB());
            var contentId = secondary.UploadFile(testFile);

            secondary.Marketplace.RequestStorage(contentId,
                pricePerBytePerSecond: 2.TestTokens(),
                requiredCollateral: 10.TestTokens(),
                minRequiredNumberOfNodes: 1,
                proofProbability: 5,
                duration: TimeSpan.FromMinutes(2));

            Time.Sleep(TimeSpan.FromMinutes(1));

            primary.Marketplace.AssertThatBalance(Is.LessThan(234.TestTokens()), "Collateral was not placed.");

            Time.Sleep(TimeSpan.FromMinutes(2));

            primary.Marketplace.AssertThatBalance(Is.GreaterThan(234.TestTokens()), "Storer was not paid for storage.");
            secondary.Marketplace.AssertThatBalance(Is.LessThan(1000.TestTokens()), "Contractor was not charged for storage.");
        }
    }
}
