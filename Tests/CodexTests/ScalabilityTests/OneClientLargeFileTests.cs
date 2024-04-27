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

            var times = new List<TimeSpan>();
            for (var i = 0; i < 100; i++)
            {
                Thread.Sleep(1000);
                var file = GenerateTestFile(100.MB());
                times.Add(Stopwatch.Measure(GetTestLog(), "Upload_" + i, () =>
                {
                    node.UploadFile(file);
                }));
            }

            Log("Upload times:");
            foreach (var t in times)
            {
                Log(Time.FormatDuration(t));
            }
        }
    }
}
