using CodexPlugin;
using CodexTests;
using FileUtils;
using NUnit.Framework;
using System.Diagnostics;
using Utils;

namespace CodexReleaseTests.DataTests
{
    public class InterruptUploadTest : CodexDistTest
    {
        [Test]
        public void UploadInterruptTest()
        {
            var nodes = StartCodex(10);

            var tasks = nodes.Select(n => Task<bool>.Run(() => RunInterruptUploadTest(n)));
            Task.WaitAll(tasks.ToArray());

            Assert.That(tasks.Select(t => t.Result).All(r => r == true));

            WaitAndCheckNodesStaysAlive(TimeSpan.FromMinutes(2), nodes);
        }

        private bool RunInterruptUploadTest(ICodexNode node)
        {
            var file = GenerateTestFile(300.MB());

            var process = StartCurlUploadProcess(node, file);

            Thread.Sleep(500);
            process.Kill();
            Thread.Sleep(1000);

            var log = Ci.DownloadLog(node);
            return !log.GetLinesContaining("Unhandled exception in async proc, aborting").Any();
        }

        private Process StartCurlUploadProcess(ICodexNode node, TrackedFile file)
        {
            var apiAddress = node.GetApiEndpoint();
            var codexUrl = $"{apiAddress}/api/codex/v1/data";
            var filePath = file.Filename;
            return Process.Start("curl", $"-X POST {codexUrl} -H \"Content-Type: application/octet-stream\" -T {filePath}");
        }
    }
}
