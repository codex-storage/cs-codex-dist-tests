using CodexPlugin;
using NUnit.Framework;
using Utils;

namespace CodexTests.BasicTests
{
    [TestFixture]
    public class LogHelperTests : AutoBootstrapDistTest
    {
        [Test]
        public void FindMostCommonLogMessages()
        {
            var uploader = AddCodex(s => s.WithName("uploader").WithLogLevel(CodexLogLevel.Trace));
            var downloader = AddCodex(s => s.WithName("downloader").WithLogLevel(CodexLogLevel.Trace));


            var cid = uploader.UploadFile(GenerateTestFile(100.MB()));

            Thread.Sleep(1000);
            var logStartUtc = DateTime.UtcNow;
            Thread.Sleep(1000);

            downloader.DownloadContent(cid);


            var map = GetLogMap(downloader, logStartUtc).OrderByDescending(p => p.Value);
            Log("Downloader - Receive");
            foreach (var entry in map)
            {
                if (entry.Value > 9)
                {
                    Log($"'{entry.Key}' = {entry.Value}");
                }
            }
        }

        private Dictionary<string, int> GetLogMap(ICodexNode node, DateTime? startUtc = null)
        {
            var log = Ci.DownloadLog(node);
            var map = new Dictionary<string, int>();
            log.IterateLines(line =>
            {
                if (string.IsNullOrEmpty(line) ||
                    !line.Contains(" ") ||
                    !line.Contains("=") ||
                    line.Length < 34 ||
                    line[33] != ' '
                ) return;

                if (startUtc.HasValue)
                {
                    var timestampLine = line.Substring(4, 23);
                    var timestamp = DateTime.Parse(timestampLine);
                    if (timestamp < startUtc) return;
                }
                
                // "INF 2024-04-14 10:40:50.042+00:00 Creating a private key and saving it       tid=1 count=2"
                var start = 34;
                var msg = line.Substring(start);

                // "Creating a private key and saving it       tid=1 count=2"
                var firstEqualSign = msg.IndexOf("=");
                msg = msg.Substring(0, firstEqualSign);

                // "Creating a private key and saving it       tid"
                var lastSpace = msg.LastIndexOf(" ");
                msg = msg.Substring(0, lastSpace);

                // "Creating a private key and saving it       "
                msg = msg.Trim();

                // "Creating a private key and saving it"
                if (map.ContainsKey(msg)) map[msg] += 1;
                else map.Add(msg, 1);
            });
            return map;
        }
    }
}
