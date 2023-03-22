using NUnit.Framework;

namespace CodexDistTestCore
{
    public interface IOnlineCodexNode
    {
        CodexDebugResponse GetDebugInfo();
        ContentId UploadFile(TestFile file);
        TestFile? DownloadContent(ContentId contentId);
    }

    public class OnlineCodexNode : IOnlineCodexNode
    {
        private readonly TestLog log;
        private readonly IFileManager fileManager;

        public OnlineCodexNode(TestLog log, IFileManager fileManager, CodexNodeContainer container)
        {
            this.log = log;
            this.fileManager = fileManager;
            Container = container;
        }

        public CodexNodeContainer Container { get; }

        public CodexDebugResponse GetDebugInfo()
        {
            var response = Http().HttpGetJson<CodexDebugResponse>("debug/info");
            Log("Got DebugInfo with id: " + response.id);
            return response;
        }

        public ContentId UploadFile(TestFile file)
        {
            Log($"Uploading file of size {file.GetFileSize()}");
            using var fileStream = File.OpenRead(file.Filename);
            var response = Http().HttpPostStream("upload", fileStream);
            if (response.StartsWith("Unable to store block"))
            {
                Assert.Fail("Node failed to store block.");
            }
            Log($"Uploaded file. Received contentId: {response}");
            return new ContentId(response);
        }

        public TestFile? DownloadContent(ContentId contentId)
        {
            Log($"Downloading for contentId: {contentId}");
            var file = fileManager.CreateEmptyTestFile();
            using var fileStream = File.OpenWrite(file.Filename);
            using var downloadStream = Http().HttpGetStream("download/" + contentId.Id);
            downloadStream.CopyTo(fileStream);
            Log($"Downloaded file of size {file.GetFileSize()}");
            return file;
        }

        private Http Http()
        {
            return new Http(ip: "127.0.0.1", port: Container.ServicePort, baseUrl: "/api/codex/v1");
        }

        private void Log(string msg)
        {
            log.Log($"Node {Container.Name}: {msg}");
        }
    }

    public class ContentId
    {
        public ContentId(string id)
        {
            Id = id;
        }

        public string Id { get; }
    }
}
