using CodexPlugin;
using DistTestCore;
using FileUtils;
using NUnit.Framework;
using Utils;

namespace CodexTests.ScalabilityTests;

[TestFixture]
public class ScalabilityTests : CodexDistTest
{
    /// <summary>
    /// We upload a file to node A, then download it with B.
    /// Then we stop node A, and download again with node C.
    /// </summary>
    [Test]
    [Combinatorial]
    [UseLongTimeouts]
    [DontDownloadLogs]
    [WaitForCleanup]
    public void ShouldMaintainFileInNetwork(
        [Values(10)] int numberOfNodes, // TODO: include 40, 80 and 100
        [Values(5000, 10000)] int fileSizeInMb // TODO: include 100, 1000
    )
    {
        var logLevel = CodexLogLevel.Info;

        var bootstrap = StartCodex(s => s.WithLogLevel(logLevel));
        var nodes = StartCodex(numberOfNodes - 1, s => s
            //.EnableMetrics()
            .WithBootstrapNode(bootstrap)
            .WithLogLevel(logLevel)
            .WithStorageQuota((fileSizeInMb + 50).MB())
        ).ToList();

        var uploader = nodes.PickOneRandom();
        var downloader = nodes.PickOneRandom();
        //var metrics = Ci.GetMetricsFor(uploader, downloader);

        var testFile = GenerateTestFile(fileSizeInMb.MB());

        LogNodeStatus(uploader);
        var contentId = uploader.UploadFile(testFile, f => LogNodeStatus(uploader));

        LogNodeStatus(downloader);
        var downloadedFile = downloader.DownloadContent(contentId, f => LogNodeStatus(downloader));

        downloadedFile!.AssertIsEqual(testFile);

        uploader.Stop(true);

        var otherDownloader = nodes.PickOneRandom();
        downloadedFile = otherDownloader.DownloadContent(contentId);

        downloadedFile!.AssertIsEqual(testFile);
    }

    /// <summary>
    /// We upload a file to each node, to put a more wide-spread load on the network.
    /// Then we run the same test as ShouldMaintainFileInNetwork.
    /// </summary>
    [Ignore("Fix ShouldMaintainFileInNetwork for all values first")]
    [Test]
    [Combinatorial]
    [UseLongTimeouts]
    [DontDownloadLogs]
    [WaitForCleanup]
    public void EveryoneGetsAFile(
        [Values(10, 40, 80, 100)] int numberOfNodes,
        [Values(100, 1000, 5000, 10000)] int fileSizeInMb
    )
    {
        var logLevel = CodexLogLevel.Info;

        var bootstrap = StartCodex(s => s.WithLogLevel(logLevel));
        var nodes = StartCodex(numberOfNodes - 1, s => s
            .WithBootstrapNode(bootstrap)
            .WithLogLevel(logLevel)
            .WithStorageQuota((fileSizeInMb + 50).MB())
        ).ToList();

        var pairTasks = nodes.Select(n =>
        {
            return Task.Run(() =>
            {
                var file = GenerateTestFile(fileSizeInMb.MB());
                var cid = n.UploadFile(file);
                return new NodeFilePair(n, file, cid);
            });
        });

        var pairs = pairTasks.Select(t => Time.Wait(t)).ToList();

        RunDoubleDownloadTest(
            pairs.PickOneRandom(),
            pairs.PickOneRandom(),
            pairs.PickOneRandom()
        );
    }

    private void RunDoubleDownloadTest(NodeFilePair source, NodeFilePair dl1, NodeFilePair dl2)
    {
        var expectedFile = source.File;
        var cid = source.Cid;

        var file1 = dl1.Node.DownloadContent(cid);
        file1!.AssertIsEqual(expectedFile);

        source.Node.Stop(true);

        var file2 = dl2.Node.DownloadContent(cid);
        file2!.AssertIsEqual(expectedFile);
    }

    public class NodeFilePair
    {
        public NodeFilePair(ICodexNode node, TrackedFile file, ContentId cid)
        {
            Node = node;
            File = file;
            Cid = cid;
        }

        public ICodexNode Node { get; }
        public TrackedFile File { get; }
        public ContentId Cid { get; }
    }
}
