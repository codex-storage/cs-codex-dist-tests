using NUnit.Framework;

namespace CodexDistTestCore
{
    public interface IOnlineCodexNode
    {
        CodexDebugResponse GetDebugInfo();
        ContentId UploadFile(TestFile file);
        TestFile? DownloadContent(ContentId contentId);
        void ConnectToPeer(IOnlineCodexNode node);
    }

    public class OnlineCodexNode : IOnlineCodexNode
    {
        private const string SuccessfullyConnectedMessage = "Successfully connected to peer";
        private const string UploadFailedMessage = "Unable to store block";

        private readonly TestLog log;
        private readonly IFileManager fileManager;

        public OnlineCodexNode(TestLog log, IFileManager fileManager, CodexNodeContainer container)
        {
            this.log = log;
            this.fileManager = fileManager;
            Container = container;
        }

        public CodexNodeContainer Container { get; }

        public string GetName()
        {
            return $"<{Container.Name}>";
        }

        public CodexDebugResponse GetDebugInfo()
        {
            var response = Http().HttpGetJson<CodexDebugResponse>("debug/info");
            Log($"Got DebugInfo with id: '{response.id}'.");
            return response;
        }

        public ContentId UploadFile(TestFile file)
        {
            Log($"Uploading file of size {file.GetFileSize()}...");
            using var fileStream = File.OpenRead(file.Filename);
            var response = Http().HttpPostStream("upload", fileStream);
            if (response.StartsWith(UploadFailedMessage))
            {
                Assert.Fail("Node failed to store block.");
            }
            Log($"Uploaded file. Received contentId: '{response}'.");
            return new ContentId(response);
        }

        public TestFile? DownloadContent(ContentId contentId)
        {
            Log($"Downloading for contentId: '{contentId.Id}'...");
            var file = fileManager.CreateEmptyTestFile();
            DownloadToFile(contentId.Id, file);
            Log($"Downloaded file of size {file.GetFileSize()} to '{file.Filename}'.");
            return file;
        }

        public void ConnectToPeer(IOnlineCodexNode node)
        {
            var peer = (OnlineCodexNode)node;

            Log($"Connecting to peer {peer.GetName()}...");
            var peerInfo = node.GetDebugInfo();
            var peerId = peerInfo.id;
            var peerMultiAddress = GetPeerMultiAddress(peer, peerInfo);

            var response = Http().HttpGetString($"connect/{peerId}?addrs={peerMultiAddress}");

            Assert.That(response, Is.EqualTo(SuccessfullyConnectedMessage), "Unable to connect codex nodes.");
            Log($"Successfully connected to peer {peer.GetName()}.");
        }

        private string GetPeerMultiAddress(OnlineCodexNode peer, CodexDebugResponse peerInfo)
        {
            // Todo: If peer is in a different pod, we must replace 0.0.0.0 with the address of that pod!

            return peerInfo.addrs.First();
            // Todo: Is there a case where First address in list is not the way?
        }

        private void DownloadToFile(string contentId, TestFile file)
        {
            using var fileStream = File.OpenWrite(file.Filename);
            using var downloadStream = Http().HttpGetStream("download/" + contentId);
            downloadStream.CopyTo(fileStream);
        }

        private Http Http()
        {
            return new Http(ip: "127.0.0.1", port: Container.ServicePort, baseUrl: "/api/codex/v1");
        }

        private void Log(string msg)
        {
            log.Log($"{GetName()}: {msg}");
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
