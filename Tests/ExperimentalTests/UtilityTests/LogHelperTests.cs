using CodexClient;
using CodexTests;
using NUnit.Framework;
using Utils;

namespace ExperimentalTests.UtilityTests
{
    [TestFixture]
    public class LogHelperTests : AutoBootstrapDistTest
    {
        [Test]
        [Ignore("Used to find the most common log messages.")]
        public void FindMostCommonLogMessages()
        {
            var uploader = StartCodex(s => s.WithName("uploader").WithLogLevel(CodexLogLevel.Trace));
            var downloader = StartCodex(s => s.WithName("downloader").WithLogLevel(CodexLogLevel.Trace));

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
            var log = node.DownloadLog();
            var map = new Dictionary<string, int>();
            log.IterateLines(line =>
            {
                var log = CodexLogLine.Parse(line);
                if (log == null) return;

                if (startUtc.HasValue)
                {
                    if (log.TimestampUtc < startUtc) return;
                }

                if (map.ContainsKey(log.Message)) map[log.Message] += 1;
                else map.Add(log.Message, 1);
            });
            return map;
        }
    }
}
