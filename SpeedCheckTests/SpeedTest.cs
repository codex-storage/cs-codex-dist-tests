using CodexPlugin;
using CodexTests;
using NUnit.Framework;
using System.Diagnostics;
using System.Drawing;
using Utils;

namespace SpeedCheckTests
{
    [TestFixture]
    public class SpeedTest : CodexDistTest
    {
        [Test]
        public void Symmetric()
        {
            // Symmetric: A node always sends a reply to every message it receives.
            CodexContainerRecipe.DockerImageOverride = "thatbenbierens/nim-codex:blkex-cancelpresence-27-f";

            var uploader = StartCodex(s => s.WithName("SymUploader"));
            var downloader = StartCodex(s => s.WithName("SymDownloader").WithBootstrapNode(uploader));
            var timeTaken = PerformTest(uploader, downloader);

            Console.WriteLine($"Symmetric time: {Time.FormatDuration(timeTaken)}");

            Assert.That(timeTaken, Is.LessThan(TimeSpan.FromSeconds(10.0)), 
                $"Symmetric: Too slow. Expected less than 10 seconds but was: {Time.FormatDuration(timeTaken)}");
        }

        [Test]
        public void Asymmetric()
        {
            // Asymmetric: A node does not always send a reply when a message is received.
            CodexContainerRecipe.DockerImageOverride = "thatbenbierens/nim-codex:blkex-cancelpresence-27-s";

            var uploader = StartCodex(s => s.WithName("AsymUploader"));
            var downloader = StartCodex(s => s.WithName("AsymDownloader").WithBootstrapNode(uploader));
            var timeTaken = PerformTest(uploader, downloader);

            Console.WriteLine($"Asymmetric time: {Time.FormatDuration(timeTaken)}");

            Assert.That(timeTaken, Is.LessThan(TimeSpan.FromSeconds(10.0)),
                $"Asymmetric: Too slow. Expected less than 10 seconds but was: {Time.FormatDuration(timeTaken)}");
        }

        [Test]
        public void Binary()
        {
            // Docker image not used: Here for api check.
            CodexContainerRecipe.DockerImageOverride = "thatbenbierens/nim-codex:blkex-cancelpresence-27-f";

            var binary = "C:\\Projects\\nim-codex\\build\\codex.exe";
            if (!File.Exists(binary)) throw new Exception("TODO: Update binary path");

            var uploadInfo = new ProcessStartInfo
            {
                FileName = binary,
                Arguments = "--data-dir=upload_data " +
                "--api-port=8081 " +
                "--nat=127.0.0.1 " +
                "--disc-ip=127.0.0.1 " +
                "--disc-port=8091 " +
                "--listen-addrs=/ip4/127.0.0.1/tcp/8071",
                UseShellExecute = true,
            };

            var uploadProcess = Process.Start(uploadInfo);

            Thread.Sleep(5000);
            if (uploadProcess == null || uploadProcess.HasExited) throw new Exception("Node exited.");

            CodexAccess.UploaderOverride = new Address("http://localhost", 8081);
            var uploader = StartCodex(s => s.WithName("BinaryUploader"));
            var spr = uploader.GetSpr();
            
            var downloadProcess = Process.Start(binary,
                "--data-dir=download_data " +
                "--api-port=8082 " +
                "--nat=127.0.0.1 " +
                "--disc-ip=127.0.0.1 " +
                "--disc-port=8092 " +
                "--listen-addrs=/ip4/127.0.0.1/tcp/8072 " +
                "--bootstrap-node=" + spr
            );

            CodexAccess.DownloaderOverride = new Address("http://localhost", 8082);
            var downloader = StartCodex(s => s.WithName("BinaryDownloader"));

            var timeTaken = PerformTest(uploader, downloader);

            uploadProcess.Kill();
            downloadProcess.Kill();

            Console.WriteLine($"Binary time: {Time.FormatDuration(timeTaken)}");

            Assert.That(timeTaken, Is.LessThan(TimeSpan.FromSeconds(10.0)),
                $"Binary: Too slow. Expected less than 10 seconds but was: {Time.FormatDuration(timeTaken)}");
        }

        private TimeSpan PerformTest(ICodexNode uploader, ICodexNode downloader)
        {
            var testFile = GenerateTestFile(100.MB());
            var contentId = uploader.UploadFile(testFile);
            var (downloadedFile, timeTaken) = downloader.DownloadContentT(contentId);
            return timeTaken;
        }
    }
}
