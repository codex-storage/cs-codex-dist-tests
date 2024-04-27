using CodexPlugin;
using DistTestCore;
using Logging;
using NUnit.Framework;
using Utils;

namespace CodexTests.ScalabilityTests
{
    [TestFixture]
    public class OneClientLargeFileTests : CodexDistTest
    {
        [Test]
        [Combinatorial]
        [UseLongTimeouts]
        public void OneClientLargeFile([Values(
            256,
            512,
            1024, // GB
            2048,
            4096,
            8192,
            16384,
            32768,
            65536,
            131072
        )] int sizeMb)
        {
            var testFile = GenerateTestFile(sizeMb.MB());

            var node = AddCodex(s => s
                .WithLogLevel(CodexLogLevel.Warn)
                .WithStorageQuota((sizeMb + 10).MB())
            );
            var contentId = node.UploadFile(testFile);
            var downloadedFile = node.DownloadContent(contentId);

            testFile.AssertIsEqual(downloadedFile);
        }

        [Test]
        public void ManyFiles()
        {
            // I suspect that the upload speed is linked to the total
            // number of blocks already in the node. I suspect the
            // metadata store to be the cause of any slow-down.
            // Using this test to detect and quantify the numbers.

            var node = AddCodex(s => s
                .WithLogLevel(CodexLogLevel.Trace)
                .WithStorageQuota(20.GB())
            );

            var startUtc = DateTime.UtcNow;
            var endUtc = DateTime.UtcNow;

            var fastMap = new Dictionary<string, int>();
            var slowMap = new Dictionary<string, int>();

            var times = new List<TimeSpan>();
            for (var i = 0; i < 100; i++)
            {
                Thread.Sleep(1000);
                var file = GenerateTestFile(100.MB());
                startUtc = DateTime.UtcNow;
                var duration = Stopwatch.Measure(GetTestLog(), "Upload_" + i, () =>
                {
                    node.UploadFile(file);
                });
                times.Add(duration);
                endUtc = DateTime.UtcNow;

                // We collect the log of the node during the upload.
                // We count the line occurances.
                // If the upload was fast, add it to the fast-map.
                // If it was slow, add it to the slow-map.
                // After the test, we can compare and hopefully see what the node was doing during the slow uploads
                // that it wasn't doing during the fast ones.
                if (duration.TotalSeconds < 12)
                {
                    AddToLogMap(fastMap, node, startUtc, endUtc);
                }
                else if (duration.TotalSeconds > 25)
                {
                    AddToLogMap(slowMap, node, startUtc, endUtc);
                }
            }

            Log("Upload times:");
            foreach (var t in times)
            {
                Log(Time.FormatDuration(t));
            }
            Log("Fast map:");
            foreach (var entry in fastMap.OrderByDescending(p => p.Value))
            {
                if (entry.Value > 9)
                {
                    Log($"'{entry.Key}' = {entry.Value}");
                }
            }
            Log("Slow map:");
            foreach (var entry in slowMap.OrderByDescending(p => p.Value))
            {
                if (entry.Value > 9)
                {
                    Log($"'{entry.Key}' = {entry.Value}");
                }
            }
        }

        private void AddToLogMap(Dictionary<string, int>  map, ICodexNode node, DateTime startUtc, DateTime endUtc)
        {
            var log = Ci.DownloadLog(node, 1000000);
            log.IterateLines(line =>
            {
                var log = CodexLogLine.Parse(line);
                if (log == null) return;
                if (log.TimestampUtc < startUtc) return;
                if (log.TimestampUtc > endUtc) return;

                if (map.ContainsKey(log.Message)) map[log.Message] += 1;
                else map.Add(log.Message, 1);
            });
        }
    }
}
