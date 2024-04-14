using CodexPlugin;
using DistTestCore;
using NUnit.Framework;
using Utils;

namespace CodexTests.ScalabilityTests;

[TestFixture]
public class ScalabilityTests : CodexDistTest
{
    private const string PatchedImage = "codexstorage/nim-codex:sha-9aeac06-dist-tests";
    private const string MasterImage = "codexstorage/nim-codex:sha-5380912-dist-tests";

    [Test]
    [Combinatorial]
    [UseLongTimeouts]
    public void ShouldMaintainFileInNetwork(
        [Values(10, 40, 80, 100)] int numberOfNodes,
        [Values(100, 1000, 5000, 10000)] int fileSizeInMb,
        [Values(true, false)] bool usePatchedImage
    )
    {
        CodexContainerRecipe.DockerImageOverride = usePatchedImage ? PatchedImage : MasterImage;

        var bootstrap = AddCodex();
        var nodes = AddCodex(numberOfNodes - 1,
            s => s.WithBootstrapNode(bootstrap).WithLogLevel(CodexLogLevel.Info)).ToList();

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
}