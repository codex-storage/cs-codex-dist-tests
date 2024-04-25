using CodexPlugin;
using DistTestCore;
using FileUtils;
using NUnit.Framework;
using Utils;

namespace CodexTests.ScalabilityTests;

[TestFixture]
public class ScalabilityTests : CodexDistTest
{
    private const string PatchedImage = "codexstorage/nim-codex:sha-9aeac06-dist-tests";
    private const string MasterImage = "codexstorage/nim-codex:sha-5380912-dist-tests";

    /// <summary>
    /// We upload a file to node A, then download it with B.
    /// Then we stop node A, and download again with node C.
    /// </summary>
    [Test]
    [Combinatorial]
    [UseLongTimeouts]
    [DontDownloadLogs]
    public void ShouldMaintainFileInNetwork(
        [Values(10, 40, 80, 100)] int numberOfNodes,
        [Values(100, 1000, 5000, 10000)] int fileSizeInMb,
        [Values(true, false)] bool usePatchedImage
    )
    {
        CodexContainerRecipe.DockerImageOverride = usePatchedImage ? PatchedImage : MasterImage;

        var logLevel = CodexLogLevel.Info;

        var bootstrap = AddCodex(s => s.WithLogLevel(logLevel));
        var nodes = AddCodex(numberOfNodes - 1, s => s
            .WithBootstrapNode(bootstrap)
            .WithLogLevel(logLevel)
            .WithStorageQuota((fileSizeInMb + 50).MB())
        ).ToList();

        var uploader = nodes.PickOneRandom();
        var downloader = nodes.PickOneRandom();

        var testFile = GenerateTestFile(fileSizeInMb.MB());
        var contentId = uploader.UploadFile(testFile);
        var downloadedFile = downloader.DownloadContent(contentId);

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
    [Ignore("Make ShouldMaintainFileInNetwork pass reliably first.")]
    [Test]
    [Combinatorial]
    [UseLongTimeouts]
    [DontDownloadLogs]
    public void EveryoneGetsAFile(
        [Values(10, 40, 80, 100)] int numberOfNodes,
        [Values(100, 1000)] int fileSizeInMb,
        [Values(true, false)] bool usePatchedImage
    )
    {
        CodexContainerRecipe.DockerImageOverride = usePatchedImage ? PatchedImage : MasterImage;

        var logLevel = CodexLogLevel.Info;

        var bootstrap = AddCodex(s => s.WithLogLevel(logLevel));
        var nodes = AddCodex(numberOfNodes - 1, s => s
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
