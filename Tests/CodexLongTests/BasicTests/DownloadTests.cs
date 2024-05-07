using CodexPlugin;
using CodexTests;
using DistTestCore;
using FileUtils;
using NUnit.Framework;
using Utils;

namespace CodexLongTests.BasicTests
{
    [TestFixture]
    public class DownloadTests : AutoBootstrapDistTest
    {
        [Test]
        [Combinatorial]
        [UseLongTimeouts]
        public void ParallelDownload(
            [Values(1, 3, 5)] int numberOfFiles,
            [Values(10, 50, 100)] int filesizeMb)
        {
            var host = AddCodex();
            var client = AddCodex();

            var testfiles = new List<TrackedFile>();
            var contentIds = new List<ContentId>();
            var downloadedFiles = new List<TrackedFile?>();

            for (int i = 0; i < numberOfFiles; i++)
            {
                testfiles.Add(GenerateTestFile(filesizeMb.MB()));
                contentIds.Add(new ContentId());
                downloadedFiles.Add(null);
            }

            for (int i = 0; i < numberOfFiles; i++)
            {
                contentIds[i] = host.UploadFile(testfiles[i]);
            }

            var downloadTasks = new List<Task>();
            for (int i = 0; i < numberOfFiles; i++)
            {
                downloadTasks.Add(Task.Run(() => { downloadedFiles[i] = client.DownloadContent(contentIds[i]); }));
            }

            Task.WaitAll(downloadTasks.ToArray());

            for (int i = 0; i < numberOfFiles; i++)
            {
                testfiles[i].AssertIsEqual(downloadedFiles[i]);
            }
        }
    }
}
