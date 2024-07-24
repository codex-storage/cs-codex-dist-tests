using DistTestCore;
using NUnit.Framework;
using Utils;

namespace CodexTests.ScalabilityTests
{
    [TestFixture]
    public class MultiPeerDownloadTests : AutoBootstrapDistTest
    {
        [Test]
        [DontDownloadLogs]
        [UseLongTimeouts]
        [Combinatorial]
        public void MultiPeerDownload(
            [Values(5, 10, 20)] int numberOfHosts,
            [Values(100, 1000)] int fileSize
        )
        {
            var hosts = StartCodex(numberOfHosts, s => s.WithLogLevel(CodexPlugin.CodexLogLevel.Trace));
            var file = GenerateTestFile(fileSize.MB());
            var cid = hosts[0].UploadFile(file);
            var tailOfManifestCid = cid.Id.Substring(cid.Id.Length - 6);

            var uploadLog = Ci.DownloadLog(hosts[0]);
            var expectedNumberOfBlocks = RoundUp(fileSize.MB().SizeInBytes, 64.KB().SizeInBytes) + 1; // +1 for manifest block.
            var blockCids = uploadLog
                .FindLinesThatContain("Block Stored")
                .Select(s =>
                {
                    var start = s.IndexOf("cid=") + 4;
                    var end = s.IndexOf(" count=");
                    var len = end - start;
                    return s.Substring(start, len);
                })
                .ToArray();

            Assert.That(blockCids.Length, Is.EqualTo(expectedNumberOfBlocks));

            foreach (var h in hosts) h.DownloadContent(cid);

            var client = StartCodex(s => s.WithLogLevel(CodexPlugin.CodexLogLevel.Trace));
            var resultFile = client.DownloadContent(cid);
            resultFile!.AssertIsEqual(file);

            var downloadLog = Ci.DownloadLog(client);
            var host = string.Empty;
            var blockIndexHostMap = new Dictionary<int, string>();
            downloadLog.IterateLines(line =>
            {
                // Received blocks from peer
                // topics="codex blockexcengine"
                // tid=1
                // peer=16U*5ULEov
                // blocks="treeCid: zDzSvJTfBgds9wsRV6iB8ZVf4fL6Nynxh2hkJSyTH4j8A9QPucyU, index: 1597"
                // count=28138

                if (line.Contains("peer=") && line.Contains(" len="))
                {
                    var start = line.IndexOf("peer=") + 5;
                    var end = line.IndexOf(" len=");
                    var len = end - start;
                    host = line.Substring(start, len);
                }
                else if (!string.IsNullOrEmpty(host) && line.Contains("Storing block with key"))
                {
                    var start = line.IndexOf("cid=") + 4;
                    var end = line.IndexOf(" count=");
                    var len = end - start;
                    var blockCid = line.Substring(start, len);

                    blockCidHostMap.Add(blockCid, host);
                    host = string.Empty;
                }
            });

            var totalFetched = blockCidHostMap.Count(p => !string.IsNullOrEmpty(p.Value));
            //PrintFullMap(blockCidHostMap);
            PrintOverview(blockCidHostMap);

            Log("Expected number of blocks: " + expectedNumberOfBlocks);
            Log("Total number of block CIDs found in dataset + manifest block: " + blockCids.Length);
            Log("Total blocks fetched by hosts: " + totalFetched);
            Assert.That(totalFetched, Is.EqualTo(expectedNumberOfBlocks));
        }

        private void PrintOverview(Dictionary<string, string> blockCidHostMap)
        {
            var overview = new Dictionary<string, int>();
            foreach (var pair in blockCidHostMap)
            {
                if (!overview.ContainsKey(pair.Value)) overview.Add(pair.Value, 1);
                else overview[pair.Value]++;
            }

            Log("Blocks fetched per host:");
            foreach (var pair in overview)
            {
                Log($"Host: {pair.Key} = {pair.Value}");
            }
        }

        private void PrintFullMap(Dictionary<string, string> blockCidHostMap)
        {
            Log("Per block, host it was fetched from:");
            foreach (var pair in blockCidHostMap)
            {
                if (string.IsNullOrEmpty(pair.Value))
                {
                    Log($"block: {pair.Key} = Not seen");
                }
                else
                {
                    Log($"block: {pair.Key} = '{pair.Value}'");
                }
            }
        }

        private long RoundUp(long filesize, long blockSize)
        {
            double f = filesize;
            double b = blockSize;

            var result = Math.Ceiling(f / b);
            return Convert.ToInt64(result);
        }
    }
}
