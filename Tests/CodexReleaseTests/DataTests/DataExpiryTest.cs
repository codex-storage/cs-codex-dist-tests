using CodexContractsPlugin;
using CodexPlugin;
using CodexTests;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.DataTests
{
    [TestFixture]
    public class DataExpiryTest : CodexDistTest
    {
        private readonly TimeSpan blockTtl = TimeSpan.FromMinutes(1.0);
        private readonly TimeSpan blockInterval = TimeSpan.FromSeconds(10.0);
        private readonly int blockCount = 100000;

        private ICodexSetup WithFastBlockExpiry(ICodexSetup setup)
        {
            return setup
                .WithBlockTTL(blockTtl)
                .WithBlockMaintenanceInterval(blockInterval)
                .WithBlockMaintenanceNumber(blockCount);
        }

        [Test]
        public void DeletesExpiredData()
        {
            var fileSize = 100.MB();
            var node = StartCodex(s => WithFastBlockExpiry(s));

            var startSpace = node.Space();
            Assert.That(startSpace.QuotaUsedBytes, Is.EqualTo(0));

            node.UploadFile(GenerateTestFile(fileSize));
            var usedSpace = node.Space();
            var usedFiles = node.LocalFiles();
            Assert.That(usedSpace.QuotaUsedBytes, Is.GreaterThanOrEqualTo(fileSize.SizeInBytes));
            Assert.That(usedSpace.FreeBytes, Is.LessThanOrEqualTo(startSpace.FreeBytes - fileSize.SizeInBytes));
            Assert.That(usedFiles.Content.Length, Is.EqualTo(1));

            Thread.Sleep(blockTtl * 2);

            var cleanupSpace = node.Space();
            var cleanupFiles = node.LocalFiles();

            Assert.That(cleanupSpace.QuotaUsedBytes, Is.LessThan(usedSpace.QuotaUsedBytes));
            Assert.That(cleanupSpace.FreeBytes, Is.GreaterThan(usedSpace.FreeBytes));
            Assert.That(cleanupFiles.Content.Length, Is.EqualTo(0));

            Assert.That(cleanupSpace.QuotaUsedBytes, Is.EqualTo(startSpace.QuotaUsedBytes));
            Assert.That(cleanupSpace.FreeBytes, Is.EqualTo(startSpace.FreeBytes));
        }

        [Test]
        public void DeletesExpiredDataUsedByStorageRequests()
        {
            var fileSize = 100.MB();

            var bootstrapNode = StartCodex();
            var geth = StartGethNode(s => s.IsMiner());
            var contracts = Ci.StartCodexContracts(geth, bootstrapNode.Version);
            var node = StartCodex(s => WithFastBlockExpiry(s)
                .EnableMarketplace(geth, contracts, m => m.WithInitial(100.Eth(), 100.Tst()))
            );

            var startSpace = node.Space();
            Assert.That(startSpace.QuotaUsedBytes, Is.EqualTo(0));

            var cid = node.UploadFile(GenerateTestFile(fileSize));
            var purchase = node.Marketplace.RequestStorage(new CodexClient.StoragePurchaseRequest(cid)
            {
                Duration = TimeSpan.FromHours(1.0),
                Expiry = blockTtl,
                MinRequiredNumberOfNodes = 3,
                NodeFailureTolerance = 1,
                PricePerBytePerSecond = 1000.TstWei(),
                ProofProbability = 20,
                CollateralPerByte = 100.TstWei()
            });
            var usedSpace = node.Space();
            var usedFiles = node.LocalFiles();
            Assert.That(usedSpace.QuotaUsedBytes, Is.GreaterThanOrEqualTo(fileSize.SizeInBytes));
            Assert.That(usedSpace.FreeBytes, Is.LessThanOrEqualTo(startSpace.FreeBytes - fileSize.SizeInBytes));
            Assert.That(usedFiles.Content.Length, Is.EqualTo(2));

            Thread.Sleep(blockTtl * 2);

            var cleanupSpace = node.Space();
            var cleanupFiles = node.LocalFiles();

            Assert.That(cleanupSpace.QuotaUsedBytes, Is.LessThan(usedSpace.QuotaUsedBytes));
            Assert.That(cleanupSpace.FreeBytes, Is.GreaterThan(usedSpace.FreeBytes));
            Assert.That(cleanupFiles.Content.Length, Is.EqualTo(0));

            Assert.That(cleanupSpace.QuotaUsedBytes, Is.EqualTo(startSpace.QuotaUsedBytes));
            Assert.That(cleanupSpace.FreeBytes, Is.EqualTo(startSpace.FreeBytes));
        }

        [Test]
        [Ignore("Issue not fixed. Ticket: https://github.com/codex-storage/nim-codex/issues/1291")]
        public void StorageRequestsKeepManifests()
        {
            var bootstrapNode = StartCodex(s => s.WithName("Bootstrap"));
            var geth = StartGethNode(s => s.IsMiner());
            var contracts = Ci.StartCodexContracts(geth, bootstrapNode.Version);
            var client = StartCodex(s => WithFastBlockExpiry(s)
                .WithName("client")
                .WithBootstrapNode(bootstrapNode)
                .EnableMarketplace(geth, contracts, m => m.WithInitial(100.Eth(), 100.Tst()))
            );

            var hosts = StartCodex(3, s => WithFastBlockExpiry(s)
                .WithName("host")
                .WithBootstrapNode(bootstrapNode)
                .EnableMarketplace(geth, contracts, m => m.AsStorageNode().WithInitial(100.Eth(), 100.Tst()))
            );
            foreach (var host in hosts) host.Marketplace.MakeStorageAvailable(new CodexClient.CreateStorageAvailability(
                totalSpace: 2.GB(),
                maxDuration: TimeSpan.FromDays(2.0),
                minPricePerBytePerSecond: 1.TstWei(),
                totalCollateral: 10.Tst()));

            var uploadCid = client.UploadFile(GenerateTestFile(5.MB()));
            var request = client.Marketplace.RequestStorage(new CodexClient.StoragePurchaseRequest(uploadCid)
            {
                CollateralPerByte = 1.TstWei(),
                Duration = TimeSpan.FromDays(1.0),
                Expiry = TimeSpan.FromHours(1.0),
                MinRequiredNumberOfNodes = 3,
                NodeFailureTolerance = 1,
                PricePerBytePerSecond = 10.TstWei(),
                ProofProbability = 99999
            });
            request.WaitForStorageContractSubmitted();
            request.WaitForStorageContractStarted();
            var storeCid = request.ContentId;

            var clientManifest = client.DownloadManifestOnly(storeCid);
            Assert.That(clientManifest.Manifest.Protected, Is.True);

            client.Stop(waitTillStopped: true);
            Thread.Sleep(blockTtl * 2.0);

            var checker = StartCodex(s => s.WithName("checker").WithBootstrapNode(bootstrapNode));
            var manifest = checker.DownloadManifestOnly(storeCid);
            Assert.That(manifest.Manifest.Protected, Is.True);
        }
    }
}
