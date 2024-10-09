using CodexPlugin;
using FileUtils;
using NUnit.Framework;
using System.Diagnostics;
using Utils;

namespace CodexTests.BasicTests
{
    [TestFixture]
    public class OneClientTests : CodexDistTest
    {
        [Test]
        public void OneClientTest()
        {
            var node = StartCodex();

            PerformOneClientTest(node);

            LogNodeStatus(node);
        }

        [Test]
        public void InterruptUploadTest()
        {
            var tasks = new List<Task<bool>>();
            for (var i = 0; i < 10; i++)
            {
                tasks.Add(Task<bool>.Run(() => RunInterruptUploadTest()));
            }
            Task.WaitAll(tasks.ToArray());

            Assert.That(tasks.Select(t => t.Result).All(r => r == true));
        }

        private bool RunInterruptUploadTest()
        {
            var node = StartCodex();
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
            var apiAddress = node.Container.GetAddress(CodexContainerRecipe.ApiPortTag);
            var codexUrl = $"{apiAddress}/api/codex/v1/data";
            var filePath = file.Filename;
            return Process.Start("curl", $"-X POST {codexUrl} -H \"Content-Type: application/octet-stream\" -T {filePath}");
        }

        private void PerformOneClientTest(ICodexNode primary)
        {
            var testFile = GenerateTestFile(1.MB());

            var contentId = primary.UploadFile(testFile);

            var downloadedFile = primary.DownloadContent(contentId);

            testFile.AssertIsEqual(downloadedFile);
        }
    }
}
