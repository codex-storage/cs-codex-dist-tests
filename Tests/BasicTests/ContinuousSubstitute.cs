using DistTestCore;
using Logging;
using NUnit.Framework;
using Utils;

namespace Tests.BasicTests
{
    [TestFixture]
    public class ContinuousSubstitute : AutoBootstrapDistTest
    {
        [Test]
        [UseLongTimeouts]
        public void ContinuousTestSubstitute()
        {
            var group = SetupCodexNodes(5, o => o
                    .EnableMetrics()
                    .EnableMarketplace(100000.TestTokens(), 0.Eth(), isValidator: true)
                    .WithBlockTTL(TimeSpan.FromMinutes(2))
                    .WithStorageQuota(3.GB()));

            var nodes = group.Cast<OnlineCodexNode>().ToArray();

            foreach (var node in nodes)
            {
                node.Marketplace.MakeStorageAvailable(
                size: 1.GB(),
                minPricePerBytePerSecond: 1.TestTokens(),
                maxCollateral: 1024.TestTokens(),
                maxDuration: TimeSpan.FromMinutes(5));
            }

            var endTime = DateTime.UtcNow + TimeSpan.FromHours(10);
            while (DateTime.UtcNow < endTime)
            {
                var allNodes = nodes.ToList();
                var primary = allNodes.PickOneRandom();
                var secondary = allNodes.PickOneRandom();

                Log("Run Test");
                PerformTest(primary, secondary);

                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }

        private ByteSize fileSize = 80.MB();

        private void PerformTest(IOnlineCodexNode primary, IOnlineCodexNode secondary)
        {
            ScopedTestFiles(() =>
            {
                var testFile = GenerateTestFile(fileSize);

                var contentId = primary.UploadFile(testFile);

                var downloadedFile = secondary.DownloadContent(contentId);

                testFile.AssertIsEqual(downloadedFile);
            });
        }
        
        [Test]
        public void HoldMyBeerTest()
        {
            var blockExpirationTime = TimeSpan.FromMinutes(3);
            var group = SetupCodexNodes(3, o => o
                    .EnableMetrics()
                    .WithBlockTTL(blockExpirationTime)
                    .WithBlockMaintenanceInterval(TimeSpan.FromMinutes(2))
                    .WithBlockMaintenanceNumber(10000)
                    .WithStorageQuota(2000.MB()));

            var nodes = group.Cast<OnlineCodexNode>().ToArray();

            var endTime = DateTime.UtcNow + TimeSpan.FromHours(24);

            var filesize = 80.MB();
            double codexDefaultBlockSize = 31 * 64 * 33;
            var numberOfBlocks = Convert.ToInt64(Math.Ceiling(filesize.SizeInBytes / codexDefaultBlockSize));
            var sizeInBytes = filesize.SizeInBytes;
            Assert.That(numberOfBlocks, Is.EqualTo(1282));

            var startTime = DateTime.UtcNow;
            var successfulUploads = 0;
            var successfulDownloads = 0;

            while (DateTime.UtcNow < endTime)
            {
                foreach (var node in nodes)
                {
                    try
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(5));

                        ScopedTestFiles(() =>
                        {
                            var uploadStartTime = DateTime.UtcNow;
                            var file = GenerateTestFile(filesize);
                            var cid = node.UploadFile(file);

                            var cidTag = cid.Id.Substring(cid.Id.Length - 6);
                            Measure("upload-log-asserts", () =>
                            {
                                var uploadLog = node.DownloadLog(tailLines: 50000);

                                var storeLines = uploadLog.FindLinesThatContain("Stored data", "topics=\"codex node\"");
                                uploadLog.DeleteFile();

                                var storeLine = GetLineForCidTag(storeLines, cidTag);
                                AssertStoreLineContains(storeLine, numberOfBlocks, sizeInBytes);
                            });
                            successfulUploads++;

                            var uploadTimeTaken = DateTime.UtcNow - uploadStartTime;
                            if (uploadTimeTaken >= blockExpirationTime.Subtract(TimeSpan.FromSeconds(10)))
                            {
                                Assert.Fail("Upload took too long. Blocks already expired.");
                            }

                            var dl = node.DownloadContent(cid);
                            file.AssertIsEqual(dl);

                            Measure("download-log-asserts", () =>
                            {
                                var downloadLog = node.DownloadLog(tailLines: 50000);

                                var sentLines = downloadLog.FindLinesThatContain("Sent bytes", "topics=\"codex restapi\"");
                                downloadLog.DeleteFile();

                                var sentLine = GetLineForCidTag(sentLines, cidTag);
                                AssertSentLineContains(sentLine, sizeInBytes);
                            });
                            successfulDownloads++;
                        });
                    }
                    catch
                    {
                        var testDuration = DateTime.UtcNow - startTime;
                        Log("Test failed. Delaying shut-down by 30 seconds to collect metrics.");
                        Log($"Test failed after {Time.FormatDuration(testDuration)} and {successfulUploads} successful uploads and {successfulDownloads} successful downloads");
                        Thread.Sleep(TimeSpan.FromSeconds(30));
                        throw;
                    }
                }

                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }

        private void AssertSentLineContains(string sentLine, long sizeInBytes)
        {
            var tag = "bytes=";
            var token = sentLine.Substring(sentLine.IndexOf(tag) + tag.Length);
            var bytes = Convert.ToInt64(token);
            Assert.AreEqual(sizeInBytes, bytes, $"Sent bytes: Number of bytes incorrect. Line: '{sentLine}'");
        }

        private void AssertStoreLineContains(string storeLine, long numberOfBlocks, long sizeInBytes)
        {
            var tokens = storeLine.Split(" ");

            var blocksToken = GetToken(tokens, "blocks=");
            var sizeToken = GetToken(tokens, "size=");
            if (blocksToken == null) Assert.Fail("blockToken not found in " + storeLine);
            if (sizeToken == null) Assert.Fail("sizeToken not found in " + storeLine);

            var blocks = Convert.ToInt64(blocksToken);
            var size = Convert.ToInt64(sizeToken?.Replace("'NByte", ""));

            var lineLog = $" Line: '{storeLine}'";
            Assert.AreEqual(numberOfBlocks, blocks, "Stored data: Number of blocks incorrect." + lineLog);
            Assert.AreEqual(sizeInBytes, size, "Stored data: Number of blocks incorrect." + lineLog);
        }

        private string GetLineForCidTag(string[] lines, string cidTag)
        {
            var result = lines.SingleOrDefault(l => l.Contains(cidTag));
            if (result == null)
            {
                Assert.Fail($"Failed to find '{cidTag}' in lines: '{string.Join(",", lines)}'");
                throw new Exception();
            }

            return result;
        }

        private string? GetToken(string[] tokens, string tag)
        {
            var token = tokens.SingleOrDefault(t => t.StartsWith(tag));
            if (token == null) return null;
            return token.Substring(tag.Length);
        }
    }
}
