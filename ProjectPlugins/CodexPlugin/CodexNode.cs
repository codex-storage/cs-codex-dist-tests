using Core;
using FileUtils;
using GethPlugin;
using KubernetesWorkflow;
using KubernetesWorkflow.Types;
using Logging;
using MetricsPlugin;
using Utils;

namespace CodexPlugin
{
    public interface ICodexNode : IHasContainer, IHasMetricsScrapeTarget, IHasEthAddress
    {
        string GetName();
        CodexDebugResponse GetDebugInfo();
        CodexDebugPeerResponse GetDebugPeer(string peerId);
        // These debug methods are not available in master-line Codex. Use only for custom builds.
        //CodexDebugBlockExchangeResponse GetDebugBlockExchange();
        //CodexDebugRepoStoreResponse[] GetDebugRepoStore();
        ContentId UploadFile(TrackedFile file);
        TrackedFile? DownloadContent(ContentId contentId, string fileLabel = "");
        CodexLocalData[] LocalFiles();
        void ConnectToPeer(ICodexNode node);
        CodexDebugVersionResponse Version { get; }
        IMarketplaceAccess Marketplace { get; }
        CrashWatcher CrashWatcher { get; }
        PodInfo GetPodInfo();
        ITransferSpeeds TransferSpeeds { get; }
        void Stop();
    }

    public class CodexNode : ICodexNode
    {
        private const string SuccessfullyConnectedMessage = "Successfully connected to peer";
        private const string UploadFailedMessage = "Unable to store block";
        private readonly IPluginTools tools;
        private readonly EthAddress? ethAddress;
        private readonly TransferSpeeds transferSpeeds;

        public CodexNode(IPluginTools tools, CodexAccess codexAccess, CodexNodeGroup group, IMarketplaceAccess marketplaceAccess, EthAddress? ethAddress)
        {
            this.tools = tools;
            this.ethAddress = ethAddress;
            CodexAccess = codexAccess;
            Group = group;
            Marketplace = marketplaceAccess;
            Version = new CodexDebugVersionResponse();
            transferSpeeds = new TransferSpeeds();
        }

        public RunningContainer Container { get { return CodexAccess.Container; } }
        public CodexAccess CodexAccess { get; }
        public CrashWatcher CrashWatcher { get => CodexAccess.CrashWatcher; }
        public CodexNodeGroup Group { get; }
        public IMarketplaceAccess Marketplace { get; }
        public CodexDebugVersionResponse Version { get; private set; }
        public ITransferSpeeds TransferSpeeds { get => transferSpeeds; }
        public IMetricsScrapeTarget MetricsScrapeTarget
        {
            get
            {
                return new MetricsScrapeTarget(CodexAccess.Container, CodexContainerRecipe.MetricsPortTag);
            }
        }
        public EthAddress EthAddress 
        {
            get
            {
                if (ethAddress == null) throw new Exception("Marketplace is not enabled for this Codex node. Please start it with the option '.EnableMarketplace(...)' to enable it.");
                return ethAddress;
            }
        }

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

        public CodexDebugBlockExchangeResponse GetDebugBlockExchange()
        {
            return CodexAccess.GetDebugBlockExchange();
        }

        public CodexDebugRepoStoreResponse[] GetDebugRepoStore()
        {
            return CodexAccess.GetDebugRepoStore();
        }

        public ContentId UploadFile(TrackedFile file)
        {
            using var fileStream = File.OpenRead(file.Filename);

            var logMessage = $"Uploading file {file.Describe()}...";
            Log(logMessage);
            var measurement = Stopwatch.Measure(tools.GetLog(), logMessage, () =>
            {
                return CodexAccess.UploadFile(fileStream);
            });

            var response = measurement.Value;
            transferSpeeds.AddUploadSample(file.GetFilesize(), measurement.Duration);

            if (string.IsNullOrEmpty(response)) FrameworkAssert.Fail("Received empty response.");
            if (response.StartsWith(UploadFailedMessage)) FrameworkAssert.Fail("Node failed to store block.");

            Log($"Uploaded file. Received contentId: '{response}'.");
            return new ContentId(response);
        }

        public TrackedFile? DownloadContent(ContentId contentId, string fileLabel = "")
        {
            var logMessage = $"Downloading for contentId: '{contentId.Id}'...";
            Log(logMessage);
            var file = tools.GetFileManager().CreateEmptyFile(fileLabel);
            var measurement = Stopwatch.Measure(tools.GetLog(), logMessage, () => DownloadToFile(contentId.Id, file));
            transferSpeeds.AddDownloadSample(file.GetFilesize(), measurement);
            Log($"Downloaded file {file.Describe()} to '{file.Filename}'.");
            return file;
        }

        public CodexLocalData[] LocalFiles()
        {
            return CodexAccess.LocalFiles().Select(l => new CodexLocalData(new ContentId(l.cid), l.manifest)).ToArray();
        }

        public void ConnectToPeer(ICodexNode node)
        {
            var peer = (CodexNode)node;

            Log($"Connecting to peer {peer.GetName()}...");
            var peerInfo = node.GetDebugInfo();
            var response = CodexAccess.ConnectToPeer(peerInfo.id, GetPeerMultiAddress(peer, peerInfo));

            FrameworkAssert.That(response == SuccessfullyConnectedMessage, "Unable to connect codex nodes.");
            Log($"Successfully connected to peer {peer.GetName()}.");
        }

        public PodInfo GetPodInfo()
        {
            return CodexAccess.GetPodInfo();
        }

        public void Stop()
        {
            if (Group.Count() > 1) throw new InvalidOperationException("Codex-nodes that are part of a group cannot be " +
                "individually shut down. Use 'BringOffline()' on the group object to stop the group. This method is only " +
                "available for codex-nodes in groups of 1.");

            Group.BringOffline();
        }

        public void EnsureOnlineGetVersionResponse()
        {
            var debugInfo = Time.Retry(CodexAccess.GetDebugInfo, "ensure online");
            var nodePeerId = debugInfo.id;
            var nodeName = CodexAccess.Container.Name;

            if (!debugInfo.codex.IsValid())
            {
                throw new Exception($"Invalid version information received from Codex node {GetName()}: {debugInfo.codex}");
            }

            var log = tools.GetLog();
            log.AddStringReplace(nodePeerId, nodeName);
            log.AddStringReplace(debugInfo.table.localNode.nodeId, nodeName);
            Version = debugInfo.codex;
        }

        private string GetPeerMultiAddress(CodexNode peer, CodexDebugResponse peerInfo)
        {
            var multiAddress = peerInfo.addrs.First();
            // Todo: Is there a case where First address in list is not the way?

            // The peer we want to connect is in a different pod.
            // We must replace the default IP with the pod IP in the multiAddress.
            var workflow = tools.CreateWorkflow();
            var podInfo = workflow.GetPodInfo(peer.Container);

            return multiAddress.Replace("0.0.0.0", podInfo.Ip);
        }

        private void DownloadToFile(string contentId, TrackedFile file)
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
            tools.GetLog().Log($"{GetName()}: {msg}");
        }
    }

    public class ContentId
    {
        public ContentId(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public override bool Equals(object? obj)
        {
            return obj is ContentId id && Id == id.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }
    }
}
