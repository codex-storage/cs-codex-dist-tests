using NUnit.Framework;

namespace CodexDistTestCore
{
    public interface IOnlineCodexNode
    {
        CodexDebugResponse GetDebugInfo();
        ContentId UploadFile(TestFile file);
        TestFile? DownloadContent(ContentId contentId);
        IOfflineCodexNode BringOffline();
    }

    public class OnlineCodexNode : IOnlineCodexNode
    {
        private readonly IK8sManager k8SManager;
        private readonly IFileManager fileManager;
        private readonly int port;

        public OnlineCodexNode(IK8sManager k8SManager, IFileManager fileManager, int port)
        {
            this.k8SManager = k8SManager;
            this.fileManager = fileManager;
            this.port = port;
        }

        public IOfflineCodexNode BringOffline()
        {
            return k8SManager.BringOffline(this);
        }

        public CodexDebugResponse GetDebugInfo()
        {
            return Http().HttpGetJson<CodexDebugResponse>("debug/info");
        }

        public ContentId UploadFile(TestFile file)
        {
            using var fileStream = File.OpenRead(file.Filename);
            var response = Http().HttpPostStream("upload", fileStream);
            if (response.StartsWith("Unable to store block"))
            {
                Assert.Fail("Node failed to store block.");
            }
            return new ContentId(response);
        }

        public TestFile? DownloadContent(ContentId contentId)
        {
            var file = fileManager.CreateEmptyTestFile();
            using var fileStream = File.OpenWrite(file.Filename);
            using var downloadStream = Http().HttpGetStream("download/" + contentId.Id);
            downloadStream.CopyTo(fileStream);
            return file;
        }

        private Http Http()
        {
            return new Http(ip: "127.0.0.1", port: port, baseUrl: "/api/codex/v1");
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
