using DistTestCore.Codex;
using DistTestCore.Logs;
using DistTestCore.Marketplace;
using DistTestCore.Metrics;
using Logging;
using NUnit.Framework;

namespace DistTestCore
{
    public interface IOnlineCodexNode
    {
        string GetName();
        CodexDebugResponse GetDebugInfo();
        CodexDebugPeerResponse GetDebugPeer(string peerId);
        CodexDebugPeerResponse GetDebugPeer(string peerId, TimeSpan timeout);
        ContentId UploadFile(TestFile file);
        TestFile? DownloadContent(ContentId contentId, string fileLabel = "");
        void ConnectToPeer(IOnlineCodexNode node);
        ICodexNodeLog DownloadLog();
        IMetricsAccess Metrics { get; }
        IMarketplaceAccess Marketplace { get; }
        ICodexSetup BringOffline();
    }

    public class OnlineCodexNode : IOnlineCodexNode
    {
        private const string SuccessfullyConnectedMessage = "Successfully connected to peer";
        private const string UploadFailedMessage = "Unable to store block";
        private readonly TestLifecycle lifecycle;

        public OnlineCodexNode(TestLifecycle lifecycle, CodexAccess codexAccess, CodexNodeGroup group, IMetricsAccess metricsAccess, IMarketplaceAccess marketplaceAccess)
        {
            this.lifecycle = lifecycle;
            CodexAccess = codexAccess;
            Group = group;
            Metrics = metricsAccess;
            Marketplace = marketplaceAccess;
        }

        public CodexAccess CodexAccess { get; }
        public CodexNodeGroup Group { get; }
        public IMetricsAccess Metrics { get; }
        public IMarketplaceAccess Marketplace { get; }

        public string GetName()
        {
            return CodexAccess.Container.Name;
        }

        public CodexDebugResponse GetDebugInfo()
        {
            var debugInfo = CodexAccess.GetDebugInfo();
            var known = string.Join(",", debugInfo.table.nodes.Select(n => n.peerId));
            Log($"Got DebugInfo with id: '{debugInfo.id}'. This node knows: {known}");
            return debugInfo;
        }

        public CodexDebugPeerResponse GetDebugPeer(string peerId)
        {
            return CodexAccess.GetDebugPeer(peerId);
        }

        public CodexDebugPeerResponse GetDebugPeer(string peerId, TimeSpan timeout)
        {
            return CodexAccess.GetDebugPeer(peerId, timeout);
        }

        public ContentId UploadFile(TestFile file)
        {
            using var fileStream = File.OpenRead(file.Filename);

            var logMessage = $"Uploading file '{file.Describe()}' of size {file.GetFileSize()}...";
            var response = Stopwatch.Measure(lifecycle.Log, logMessage, () =>
            {
                return CodexAccess.UploadFile(fileStream);
            });

            if (response.StartsWith(UploadFailedMessage))
            {
                Assert.Fail("Node failed to store block.");
            }
            var logReplacement = $"(CID:{file.Describe()})";
            Log($"ContentId '{response}' is {logReplacement}");
            lifecycle.Log.AddStringReplace(response, logReplacement);
            Log($"Uploaded file. Received contentId: '{response}'.");
            return new ContentId(response);
        }

        public TestFile? DownloadContent(ContentId contentId, string fileLabel = "")
        {
            var logMessage = $"Downloading for contentId: '{contentId.Id}'...";
            var file = lifecycle.FileManager.CreateEmptyTestFile(fileLabel);
            Stopwatch.Measure(lifecycle.Log, logMessage, () => DownloadToFile(contentId.Id, file));
            Log($"Downloaded file '{file.Describe()}' of size {file.GetFileSize()} to '{file.Filename}'.");
            return file;
        }

        public void ConnectToPeer(IOnlineCodexNode node)
        {
            var peer = (OnlineCodexNode)node;

            Log($"Connecting to peer {peer.GetName()}...");
            var peerInfo = node.GetDebugInfo();
            var response = CodexAccess.ConnectToPeer(peerInfo.id, GetPeerMultiAddress(peer, peerInfo));

            Assert.That(response, Is.EqualTo(SuccessfullyConnectedMessage), "Unable to connect codex nodes.");
            Log($"Successfully connected to peer {peer.GetName()}.");
        }

        public ICodexNodeLog DownloadLog()
        {
            return lifecycle.DownloadLog(this);
        }

        public ICodexSetup BringOffline()
        {
            if (Group.Count() > 1) throw new InvalidOperationException("Codex-nodes that are part of a group cannot be " +
                "individually shut down. Use 'BringOffline()' on the group object to stop the group. This method is only " +
                "available for codex-nodes in groups of 1.");

            return Group.BringOffline();
        }

        private string GetPeerMultiAddress(OnlineCodexNode peer, CodexDebugResponse peerInfo)
        {
            var multiAddress = peerInfo.addrs.First();
            // Todo: Is there a case where First address in list is not the way?

            if (Group == peer.Group)
            {
                return multiAddress;
            }

            // The peer we want to connect is in a different pod.
            // We must replace the default IP with the pod IP in the multiAddress.
            return multiAddress.Replace("0.0.0.0", peer.Group.Containers.RunningPod.PodInfo.Ip);
        }

        private void DownloadToFile(string contentId, TestFile file)
        {
            using var fileStream = File.OpenWrite(file.Filename);
            try
            {
                using var downloadStream = CodexAccess.DownloadFile(contentId);
                downloadStream.CopyTo(fileStream);
            }
            catch
            {
                Log($"Failed to download file '{contentId}'.");
                throw;
            }
        }

        private void Log(string msg)
        {
            lifecycle.Log.Log($"{GetName()}: {msg}");
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
