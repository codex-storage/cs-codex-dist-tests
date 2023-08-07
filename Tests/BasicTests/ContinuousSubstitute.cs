using ContinuousTests;
using DistTestCore;
using NUnit.Framework;
using Utils;

namespace Tests.BasicTests
{
    [TestFixture]
    public class ContinuousSubstitute : AutoBootstrapDistTest
    {
        [Test]
        public void ContinuousTestSubstitute()
        {
            var nodes = new List<OnlineCodexNode>();
            for (var i = 0; i < 5; i++)
            {
                nodes.Add((OnlineCodexNode)SetupCodexNode(o => o
                    .EnableMarketplace(100000.TestTokens(), 0.Eth(), isValidator: i < 2)
                    .WithStorageQuota(2.GB())
                ));
            }

            var cts = new CancellationTokenSource();
            var ct = cts.Token;
            var dlPath = Path.Combine(new FileInfo(Get().Log.LogFile.FullFilename)!.Directory!.FullName, "continuouslogs");
            Directory.CreateDirectory(dlPath);

            var containers = nodes.Select(n => n.CodexAccess.Container).ToArray();
            var cd = new ContinuousLogDownloader(Get(), containers, dlPath, ct);

            var logTask = Task.Run(cd.Run);

            try
            {
                foreach (var node in nodes)
                {
                    node.Marketplace.MakeStorageAvailable(
                    size: 1.GB(),
                    minPricePerBytePerSecond: 1.TestTokens(),
                    maxCollateral: 1024.TestTokens(),
                    maxDuration: TimeSpan.FromMinutes(5));
                }

                var endTime = DateTime.UtcNow + TimeSpan.FromHours(1);
                while (DateTime.UtcNow < endTime)
                {
                    var allNodes = nodes.ToList();
                    var primary = allNodes.PickOneRandom();
                    var secondary = allNodes.PickOneRandom();

                    Log("Run Test");
                    PerformTest(primary, secondary);

                    Thread.Sleep(TimeSpan.FromSeconds(30));
                }
            }
            finally
            {
                cts.Cancel();
                logTask.Wait();
            }
        }

        private void PerformTest(IOnlineCodexNode primary, IOnlineCodexNode secondary)
        {
            ScopedTestFiles(() =>
            {
                var testFile = GenerateTestFile(1000.MB());

                var contentId = primary.UploadFile(testFile);

                var downloadedFile = secondary.DownloadContent(contentId);

                testFile.AssertIsEqual(downloadedFile);
            });
        }
    }
}
