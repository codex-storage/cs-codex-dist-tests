using System.Security.Cryptography;
using CodexReleaseTests.MarketTests;
using Nethereum.JsonRpc.Client;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.DataTests
{
    public class DecodeTest : MarketplaceAutoBootstrapDistTest
    {
        protected override int NumberOfHosts => 0;
        protected override int NumberOfClients => 2;
        protected override ByteSize HostAvailabilitySize => 0.Bytes();
        protected override TimeSpan HostAvailabilityMaxDuration => TimeSpan.FromSeconds(0.0);

        [Test]
        public void DecodeDataset()
        {
            var clients = StartClients();

            var file = GenerateTestFile(10.MB());
            var bCid = clients[0].UploadFile(file);
            var request = clients[0].Marketplace.RequestStorage(new CodexClient.StoragePurchaseRequest(bCid)
            {
                Expiry = TimeSpan.FromMinutes(5.0),
                Duration = TimeSpan.FromMinutes(100.0),
                CollateralPerByte = 100.Tst(),
                MinRequiredNumberOfNodes = 6,
                NodeFailureTolerance = 3,
                PricePerBytePerSecond = 100.Tst(),
                ProofProbability = 20
            });
            var eCid = request.ContentId;

            Assert.That(bCid.Id, Is.Not.EqualTo(eCid.Id));

            var basic = clients[0].DownloadManifestOnly(bCid);
            var encoded = clients[0].DownloadManifestOnly(eCid);
            Assert.That(basic.Manifest.Protected, Is.False);
            Assert.That(encoded.Manifest.Protected, Is.True);

            var decoded = clients[1].DownloadContent(eCid);

            file.AssertIsEqual(decoded);
        }

        [Test]
        [Ignore("Crashes node attempting encoding. Issue: https://github.com/codex-storage/nim-codex/issues/1185")]
        public void PartiallyDeletedDatasets()
        {
            var clients = StartClients(s => s
                .WithBlockMaintenanceNumber(1)
                .WithBlockMaintenanceInterval(TimeSpan.FromSeconds(10.0))
                .WithBlockTTL(TimeSpan.FromSeconds(30.0)));


            var file = GenerateTestFile(2.MB());
            var bCid = clients[0].UploadFile(file);

            var space = clients[0].Space();
            var update = space;
            while (space.QuotaUsedBytes == update.QuotaUsedBytes)
            {
                Thread.Sleep(TimeSpan.FromSeconds(3.0));
                update = clients[0].Space();
            }

            Assert.That(update.QuotaUsedBytes, Is.LessThan(space.QuotaUsedBytes));
            // The dataset is partially deleted.

            // What happens when we request storage for it?
            var request = clients[0].Marketplace.RequestStorage(new CodexClient.StoragePurchaseRequest(bCid)
            {
                Expiry = TimeSpan.FromMinutes(5.0),
                Duration = TimeSpan.FromMinutes(100.0),
                CollateralPerByte = 100.Tst(),
                MinRequiredNumberOfNodes = 6,
                NodeFailureTolerance = 3,
                PricePerBytePerSecond = 100.Tst(),
                ProofProbability = 20
            });
            var eCid = request.ContentId;

            Assert.That(bCid.Id, Is.Not.EqualTo(eCid.Id));

            var basic = clients[0].DownloadManifestOnly(bCid);
            var encoded = clients[0].DownloadManifestOnly(eCid);
            Assert.That(basic.Manifest.Protected, Is.False);
            Assert.That(encoded.Manifest.Protected, Is.True);

            var decoded = clients[1].DownloadContent(eCid);

            file.AssertIsEqual(decoded);
        }
    }
}
