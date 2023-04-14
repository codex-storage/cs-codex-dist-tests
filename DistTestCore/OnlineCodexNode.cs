﻿using DistTestCore.Codex;
using DistTestCore.Logs;
using DistTestCore.Metrics;
using NUnit.Framework;

namespace DistTestCore
{
    public interface IOnlineCodexNode
    {
        CodexDebugResponse GetDebugInfo();
        ContentId UploadFile(TestFile file);
        TestFile? DownloadContent(ContentId contentId);
        void ConnectToPeer(IOnlineCodexNode node);
        ICodexNodeLog DownloadLog();
        IMetricsAccess Metrics { get; }
        //IMarketplaceAccess Marketplace { get; }
    }

    public class OnlineCodexNode : IOnlineCodexNode
    {
        private const string SuccessfullyConnectedMessage = "Successfully connected to peer";
        private const string UploadFailedMessage = "Unable to store block";
        private readonly TestLifecycle lifecycle;

        public OnlineCodexNode(TestLifecycle lifecycle, CodexAccess codexAccess, CodexNodeGroup group, IMetricsAccess metricsAccess)
        {
            this.lifecycle = lifecycle;
            CodexAccess = codexAccess;
            Group = group;
            Metrics = metricsAccess;
        }

        public CodexAccess CodexAccess { get; }
        public CodexNodeGroup Group { get; }
        public IMetricsAccess Metrics { get; }

        public string GetName()
        {
            return $"<{CodexAccess.Container.Recipe.Name}>";
        }

        public CodexDebugResponse GetDebugInfo()
        {
            var response = CodexAccess.GetDebugInfo();
            Log($"Got DebugInfo with id: '{response.id}'.");
            return response;
        }

        public ContentId UploadFile(TestFile file)
        {
            Log($"Uploading file of size {file.GetFileSize()}...");
            using var fileStream = File.OpenRead(file.Filename);
            var response = CodexAccess.UploadFile(fileStream);
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
            var file = lifecycle.FileManager.CreateEmptyTestFile();
            DownloadToFile(contentId.Id, file);
            Log($"Downloaded file of size {file.GetFileSize()} to '{file.Filename}'.");
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

        public string Describe()
        {
            return $"{Group.Describe()} contains {GetName()}";
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
            return multiAddress.Replace("0.0.0.0", peer.Group.Containers.RunningPod.Ip);
        }

        private void DownloadToFile(string contentId, TestFile file)
        {
            using var fileStream = File.OpenWrite(file.Filename);
            using var downloadStream = CodexAccess.DownloadFile(contentId);
            downloadStream.CopyTo(fileStream);
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