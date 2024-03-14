using CodexContractsPlugin;
using CodexPlugin;
using GethPlugin;
using NUnit.Framework;
using Utils;

namespace CodexTests.BasicTests
{
    [TestFixture]
    public class TwoClientTests : CodexDistTest
    {
        [Test]
        [Combinatorial]
        public void TwoClient(
            [Values(0, 1, 2, 3)] int upmode,
            [Values(0, 1, 2, 3)] int downmode)
        {
            var geth = Ci.StartGethNode(g => g.IsMiner());
            var contracts = Ci.StartCodexContracts(geth);

            var uploader = AddCodex(s => 
            {
                s.WithName("Uploader");
                s.WithStorageQuota(10.GB());

                if (upmode == 1) s.EnableMarketplace(geth, contracts, 10.Eth(), 10.TestTokens());
                if (upmode > 1) s.EnableMarketplace(geth, contracts, 10.Eth(), 10.TestTokens(), s => s.AsStorageNode());
            });

            var downloader = AddCodex(s =>
            {
                s.WithName("Downloader");
                s.WithStorageQuota(10.GB());
                s.WithBootstrapNode(uploader);

                if (downmode == 1) s.EnableMarketplace(geth, contracts, 10.Eth(), 10.TestTokens());
                if (downmode > 1) s.EnableMarketplace(geth, contracts, 10.Eth(), 10.TestTokens(), s => s.AsStorageNode());
            });

            if (upmode == 3)
            {
                uploader.Marketplace.MakeStorageAvailable(
                size: 2.GB(),
                minPriceForTotalSpace: 1.TestTokens(),
                maxCollateral: 20.TestTokens(),
                maxDuration: TimeSpan.FromMinutes(3));
            }
            if (downmode == 3)
            {
                downloader.Marketplace.MakeStorageAvailable(
                size: 2.GB(),
                minPriceForTotalSpace: 1.TestTokens(),
                maxCollateral: 20.TestTokens(),
                maxDuration: TimeSpan.FromMinutes(3));
            }

            PerformTwoClientTest(uploader, downloader);
        }


        [Test]
        [Combinatorial]
        public void ConnectivityOverGit(
            [Values(0)] int upmode,
            [Values(0, 1)] int downmode,
            [Values(0, 1, 2, 3, 4, 5, 6)] int gitIndex)
        {
            var gits = new[]
            {
                ""
            };

            CodexContainerRecipe.DockerImageOverride = gits[gitIndex];

            var geth = Ci.StartGethNode(g => g.IsMiner());
            var contracts = Ci.StartCodexContracts(geth);

            var uploader = AddCodex(s =>
            {
                s.WithName("Uploader");
                s.WithStorageQuota(10.GB());

                if (upmode == 1) s.EnableMarketplace(geth, contracts, 10.Eth(), 10.TestTokens());
                if (upmode > 1) s.EnableMarketplace(geth, contracts, 10.Eth(), 10.TestTokens(), s => s.AsStorageNode());
            });

            var downloader = AddCodex(s =>
            {
                s.WithName("Downloader");
                s.WithStorageQuota(10.GB());
                s.WithBootstrapNode(uploader);

                if (downmode == 1) s.EnableMarketplace(geth, contracts, 10.Eth(), 10.TestTokens());
                if (downmode > 1) s.EnableMarketplace(geth, contracts, 10.Eth(), 10.TestTokens(), s => s.AsStorageNode());
            });

            if (upmode == 3)
            {
                uploader.Marketplace.MakeStorageAvailable(
                size: 2.GB(),
                minPriceForTotalSpace: 1.TestTokens(),
                maxCollateral: 20.TestTokens(),
                maxDuration: TimeSpan.FromMinutes(3));
            }
            if (downmode == 3)
            {
                downloader.Marketplace.MakeStorageAvailable(
                size: 2.GB(),
                minPriceForTotalSpace: 1.TestTokens(),
                maxCollateral: 20.TestTokens(),
                maxDuration: TimeSpan.FromMinutes(3));
            }

            CreatePeerConnectionTestHelpers().AssertFullyConnected(new[] { uploader, downloader });
        }



        [Test]
        [Combinatorial]
        public void Connectivity(
            [Values(0, 1, 2, 3)] int upmode,
            [Values(0, 1, 2, 3)] int downmode)
        {
            var geth = Ci.StartGethNode(g => g.IsMiner());
            var contracts = Ci.StartCodexContracts(geth);

            var uploader = AddCodex(s =>
            {
                s.WithName("Uploader");
                s.WithStorageQuota(10.GB());

                if (upmode == 1) s.EnableMarketplace(geth, contracts, 10.Eth(), 10.TestTokens());
                if (upmode > 1) s.EnableMarketplace(geth, contracts, 10.Eth(), 10.TestTokens(), s => s.AsStorageNode());
            });

            var downloader = AddCodex(s =>
            {
                s.WithName("Downloader");
                s.WithStorageQuota(10.GB());
                s.WithBootstrapNode(uploader);

                if (downmode == 1) s.EnableMarketplace(geth, contracts, 10.Eth(), 10.TestTokens());
                if (downmode > 1) s.EnableMarketplace(geth, contracts, 10.Eth(), 10.TestTokens(), s => s.AsStorageNode());
            });

            if (upmode == 3)
            {
                uploader.Marketplace.MakeStorageAvailable(
                size: 2.GB(),
                minPriceForTotalSpace: 1.TestTokens(),
                maxCollateral: 20.TestTokens(),
                maxDuration: TimeSpan.FromMinutes(3));
            }
            if (downmode == 3)
            {
                downloader.Marketplace.MakeStorageAvailable(
                size: 2.GB(),
                minPriceForTotalSpace: 1.TestTokens(),
                maxCollateral: 20.TestTokens(),
                maxDuration: TimeSpan.FromMinutes(3));
            }

            CreatePeerConnectionTestHelpers().AssertFullyConnected(new[] { uploader, downloader });
        }


        [Test]
        public void TwoClientTest()
        {
            var uploader = AddCodex(s => s.WithName("Uploader"));
            var downloader = AddCodex(s => s.WithName("Downloader").WithBootstrapNode(uploader));

            PerformTwoClientTest(uploader, downloader);
        }

        [Test]
        public void TwoClientsTwoLocationsTest()
        {
            var locations = Ci.GetKnownLocations();
            if (locations.NumberOfLocations < 2)
            {
                Assert.Inconclusive("Two-locations test requires 2 nodes to be available in the cluster.");
                return;
            }

            var uploader = Ci.StartCodexNode(s => s.At(locations.Get(0)));
            var downloader = Ci.StartCodexNode(s => s.WithBootstrapNode(uploader).At(locations.Get(1)));

            PerformTwoClientTest(uploader, downloader);
        }

        private void PerformTwoClientTest(ICodexNode uploader, ICodexNode downloader)
        {
            PerformTwoClientTest(uploader, downloader, 10.MB());
        }

        private void PerformTwoClientTest(ICodexNode uploader, ICodexNode downloader, ByteSize size)
        {
            var testFile = GenerateTestFile(size);

            var contentId = uploader.UploadFile(testFile);

            var downloadedFile = downloader.DownloadContent(contentId);

            testFile.AssertIsEqual(downloadedFile);
            CheckLogForErrors(uploader, downloader);
        }
    }
}
