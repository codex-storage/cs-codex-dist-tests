using NUnit.Framework;
using Utils;

namespace CodexTests.ScalabilityTests
{
    [TestFixture]
    public class MultiPeerDownloadTests : AutoBootstrapDistTest
    {
        [Test]
        public void MultiPeerDownload()
        {
            var hosts = AddCodex(5, s => s.WithLogLevel(CodexPlugin.CodexLogLevel.Trace));
            var file = GenerateTestFile(100.MB());
            var cid = hosts[0].UploadFile(file);

            var uploadLog = Ci.DownloadLog(hosts[0]);
            var blockCids = uploadLog
                .FindLinesThatContain("Putting block into network store")
                .Select(s =>
                {
                    var start = s.IndexOf("cid=") + 4;
                    var end = s.IndexOf(" count=");
                    var len = end - start;
                    return s.Substring(start, len);
                })
                .ToArray();

            // Each host has the file.
            foreach (var h in hosts) h.DownloadContent(cid);

            var client = AddCodex(s => s.WithLogLevel(CodexPlugin.CodexLogLevel.Trace));
            var resultFile = client.DownloadContent(cid);
            resultFile!.AssertIsEqual(file);

            var downloadLog = Ci.DownloadLog(client);
            var blocksPerHost = new Dictionary<string, int>();
            var seenBlocks = new List<string>();
            var host = string.Empty;
            downloadLog.IterateLines(line =>
            {
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

                    if (!seenBlocks.Contains(blockCid))
                    {
                        seenBlocks.Add(blockCid);
                        if (!blocksPerHost.ContainsKey(host)) blocksPerHost.Add(host, 1);
                        else blocksPerHost[host]++;
                    }
                }
            });

            Log("Total number of blocks in dataset: " + blockCids.Length);
            Log("Blocks fetched per host:");
            foreach (var pair in blocksPerHost)
            {
                Log($"Host: {pair.Key} = {pair.Value}");
            }
        }
    }
}
