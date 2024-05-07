using CodexPlugin;
using CodexTests;
using DistTestCore;
using FileUtils;
using NUnit.Framework;
using Utils;

namespace CodexLongTests.BasicTests
{
    [TestFixture]
    public class UploadTests : AutoBootstrapDistTest
    {
        [Test]
        [Combinatorial] 
        [UseLongTimeouts]
        public void ParallelUpload(
            [Values(1, 3, 5)] int numberOfFiles,
            [Values(10, 50, 100)] int filesizeMb)
        {
            var host = AddCodex();
            var client = AddCodex();

            var testfiles = new List<TrackedFile>();
            var contentIds = new List<ContentId>();

            for (int i = 0; i < numberOfFiles; i++)
            {
                testfiles.Add(GenerateTestFile(filesizeMb.MB()));
                contentIds.Add(new ContentId());
            }

            var uploadTasks = new List<Task>();
            for (int i = 0; i < numberOfFiles; i++)
            {
                uploadTasks.Add(Task.Run(() => { contentIds[i] = host.UploadFile(testfiles[i]); }));
            }

            Task.WaitAll(uploadTasks.ToArray());

            for (int i = 0; i < numberOfFiles; i++)
            {
                var downloaded = client.DownloadContent(contentIds[i]);
                testfiles[i].AssertIsEqual(downloaded);
            }
        }
    }
}
