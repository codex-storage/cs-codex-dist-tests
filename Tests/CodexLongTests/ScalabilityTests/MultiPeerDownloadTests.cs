using CodexClient;
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
            var hosts = StartCodex(numberOfHosts, s => s.WithName("host").WithLogLevel(CodexLogLevel.Trace));
            var file = GenerateTestFile(fileSize.MB());

            var uploadTasks = hosts.Select(h =>
            {
                return Task.Run(() =>
                {
                    return h.UploadFile(file);
                });
            }).ToArray();

            Task.WaitAll(uploadTasks);
            var cid = new ContentId(uploadTasks.Select(t => t.Result.Id).Distinct().Single());

            var uploadLog = hosts[0].DownloadLog();
            var expectedNumberOfBlocks = RoundUp(fileSize.MB().SizeInBytes, 64.KB().SizeInBytes) + 1; // +1 for manifest block.
            var blockCids = uploadLog
                .FindLinesThatContain("Block Stored")
                .Select(s =>
                {
                    var line = CodexLogLine.Parse(s)!;
                    return line.Attributes["cid"];
                })
                .ToArray();

            Assert.That(blockCids.Length, Is.EqualTo(expectedNumberOfBlocks));



            var client = StartCodex(s => s.WithName("client").WithLogLevel(CodexLogLevel.Trace));
            var resultFile = client.DownloadContent(cid);
            resultFile!.AssertIsEqual(file);

            var downloadLog = client.DownloadLog();
            var host = string.Empty;
            var blockAddressHostMap = new Dictionary<string, List<string>>();
            downloadLog
                .IterateLines(s =>
            {
                var line = CodexLogLine.Parse(s)!;
                var peer = line.Attributes["peer"];
                var blockAddresses = line.Attributes["blocks"];

                AddBlockAddresses(peer, blockAddresses, blockAddressHostMap);

            }, thatContain: "Received blocks from peer");

            var totalFetched = blockAddressHostMap.Count(p => p.Value.Any());
            PrintFullMap(blockAddressHostMap);
            //PrintOverview(blockCidHostMap);

            Log("Expected number of blocks: " + expectedNumberOfBlocks);
            Log("Total number of block CIDs found in dataset + manifest block: " + blockCids.Length);
            Log("Total blocks fetched by hosts: " + totalFetched);
            Assert.That(totalFetched, Is.EqualTo(expectedNumberOfBlocks));
        }

        private void AddBlockAddresses(string peer, string blockAddresses, Dictionary<string, List<string>> blockAddressHostMap)
        {
            // Single line can contain multiple block addresses.
            var tokens = blockAddresses.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList();
            while (tokens.Count > 0)
            {
                if (tokens.Count == 1)
                {
                    AddBlockAddress(peer, tokens[0], blockAddressHostMap);
                    return;
                }

                var blockAddress = $"{tokens[0]}, {tokens[1]}";
                tokens.RemoveRange(0, 2);

                AddBlockAddress(peer, blockAddress, blockAddressHostMap);
            }
        }

        private void AddBlockAddress(string peer, string blockAddress, Dictionary<string, List<string>> blockAddressHostMap)
        {
            if (blockAddressHostMap.ContainsKey(blockAddress))
            {
                blockAddressHostMap[blockAddress].Add(peer);
            }
            else
            {
                blockAddressHostMap[blockAddress] = new List<string> { peer };
            }
        }

        private void PrintOverview(Dictionary<string, List<string>> blockAddressHostMap)
        {
            var overview = new Dictionary<string, int>();
            foreach (var pair in blockAddressHostMap)
            {
                foreach (var host in pair.Value)
                {
                    if (!overview.ContainsKey(host)) overview.Add(host, 1);
                    else overview[host]++;
                }
            }

            Log("Blocks fetched per host:");
            foreach (var pair in overview)
            {
                Log($"Host: {pair.Key} = {pair.Value}");
            }
        }

        private void PrintFullMap(Dictionary<string, List<string>> blockAddressHostMap)
        {
            Log("Per block, host it was fetched from:");
            foreach (var pair in blockAddressHostMap)
            {
                var hostStr = $"[{string.Join(",", pair.Value)}]";
                Log($"blockAddress: {pair.Key} = {hostStr}");
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
