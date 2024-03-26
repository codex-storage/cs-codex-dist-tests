using CodexContractsPlugin;
using CodexPlugin;
using GethPlugin;
using KubernetesWorkflow.Types;
using Logging;
using MetricsPlugin;
using NUnit.Framework;
using Utils;

namespace CodexTests.BasicTests
{
    [Ignore("Used for debugging continuous tests")]
    [TestFixture]
    public class ContinuousSubstitute : AutoBootstrapDistTest
    {
        [Test]
        public void ContinuousTestSubstitute()
        {
            var geth = Ci.StartGethNode(s => s.IsMiner().WithName("geth"));
            var contract = Ci.StartCodexContracts(geth);

            var group = AddCodex(5, o => o
                    .EnableMetrics()
                    .EnableMarketplace(geth, contract, 10.Eth(), 100000.TestTokens(), s => s
                        .AsStorageNode()
                        .AsValidator())
                    .WithBlockTTL(TimeSpan.FromMinutes(5))
                    .WithBlockMaintenanceInterval(TimeSpan.FromSeconds(10))
                    .WithBlockMaintenanceNumber(100)
                    .WithStorageQuota(1.GB()));

            var nodes = group.Cast<CodexNode>().ToArray();

            var rc = Ci.DeployMetricsCollector(nodes);

            var availability = new StorageAvailability(
                totalSpace: 500.MB(),
                maxDuration: TimeSpan.FromMinutes(5),
                minPriceForTotalSpace: 500.TestTokens(),
                maxCollateral: 1024.TestTokens()
            );

            foreach (var node in nodes)
            {
                node.Marketplace.MakeStorageAvailable(availability);
            }

            var endTime = DateTime.UtcNow + TimeSpan.FromHours(10);
            while (DateTime.UtcNow < endTime)
            {
                var allNodes = nodes.ToList();
                var primary = allNodes.PickOneRandom();
                var secondary = allNodes.PickOneRandom();

                Log("Run Test");
                PerformTest(primary, secondary, rc);

                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }

        private void LogBytesPerMillisecond(Action action)
        {
            var sw = Stopwatch.Begin(GetTestLog());
            action();
            var duration = sw.End();
            double totalMs = duration.TotalMilliseconds;
            double totalBytes = fileSize.SizeInBytes;

            var bytesPerMs = totalBytes / totalMs;
            Log($"Bytes per millisecond: {bytesPerMs}");
        }

        [Test]
        public void PeerTest()
        {
            var group = AddCodex(5, o => o
                    //.EnableMetrics()
                    //.EnableMarketplace(100000.TestTokens(), 0.Eth(), isValidator: true)
                    .WithBlockTTL(TimeSpan.FromMinutes(2))
                    .WithBlockMaintenanceInterval(TimeSpan.FromMinutes(2))
                    .WithBlockMaintenanceNumber(10000)
                    .WithBlockTTL(TimeSpan.FromMinutes(2))
                    .WithStorageQuota(1.GB()));

            var nodes = group.Cast<CodexNode>().ToArray();

            var checkTime = DateTime.UtcNow + TimeSpan.FromMinutes(1);
            var endTime = DateTime.UtcNow + TimeSpan.FromHours(10);
            while (DateTime.UtcNow < endTime)
            {
                //CreatePeerConnectionTestHelpers().AssertFullyConnected(GetAllOnlineCodexNodes());
                //CheckRoutingTables(GetAllOnlineCodexNodes());

                var node = nodes.ToList().PickOneRandom();
                var file = GenerateTestFile(50.MB());
                node.UploadFile(file);

                Thread.Sleep(20000);
            }
        }

        private void CheckRoutingTables(IEnumerable<ICodexNode> nodes)
        {
            var all = nodes.ToArray();
            var allIds = all.Select(n => n.GetDebugInfo().Table.LocalNode.NodeId).ToArray();

            var errors = all.Select(n => AreAllPresent(n, allIds)).Where(s => !string.IsNullOrEmpty(s)).ToArray();

            if (errors.Any())
            {
                Assert.Fail(string.Join(Environment.NewLine, errors));
            }
        }

        private string AreAllPresent(ICodexNode n, string[] allIds)
        {
            var info = n.GetDebugInfo();
            var known = info.Table.Nodes.Select(n => n.NodeId).ToArray();
            var expected = allIds.Where(i => i != info.Table.LocalNode.NodeId).ToArray();

            if (!expected.All(ex => known.Contains(ex)))
            {
                return $"Not all of '{string.Join(",", expected)}' were present in routing table: '{string.Join(",", known)}'";
            }

            return string.Empty;
        }

        private ByteSize fileSize = 80.MB();

        private const string BytesStoredMetric = "codexRepostoreBytesUsed";

        private void PerformTest(ICodexNode primary, ICodexNode secondary, RunningContainers rc)
        {
            ScopedTestFiles(() =>
            {
                var testFile = GenerateTestFile(fileSize);

                var metrics = Ci.WrapMetricsCollector(rc, primary);
                var beforeBytesStored = metrics.GetMetric(BytesStoredMetric);

                ContentId contentId = null!;
                LogBytesPerMillisecond(() => contentId = primary.UploadFile(testFile));

                var low = fileSize.SizeInBytes;
                var high = low * 1.2;
                Log("looking for: " + low + " < " + high);

                Time.WaitUntil(() =>
                {
                    var afterBytesStored = metrics.GetMetric(BytesStoredMetric);
                    var newBytes = Convert.ToInt64(afterBytesStored.Values.Last().Value - beforeBytesStored.Values.Last().Value);

                    return high > newBytes && newBytes > low;
                }, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(2));

                FileUtils.TrackedFile? downloadedFile = null;
                LogBytesPerMillisecond(() => downloadedFile = secondary.DownloadContent(contentId));

                testFile.AssertIsEqual(downloadedFile);
            });
        }

        [Test]
        public void HoldMyBeerTest()
        {
            var blockExpirationTime = TimeSpan.FromMinutes(3);
            var group = AddCodex(3, o => o
                    .EnableMetrics()
                    .WithBlockTTL(blockExpirationTime)
                    .WithBlockMaintenanceInterval(TimeSpan.FromMinutes(2))
                    .WithBlockMaintenanceNumber(10000)
                    .WithStorageQuota(2000.MB()));

            var nodes = group.Cast<CodexNode>().ToArray();

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
                                var uploadLog = Ci.DownloadLog(node, tailLines: 50000);

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
                                var downloadLog = Ci.DownloadLog(node, tailLines: 50000);

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
