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
        DebugInfo GetDebugInfo();
        DebugPeer GetDebugPeer(string peerId);
        ContentId UploadFile(TrackedFile file);
        ContentId UploadFile(TrackedFile file, Action<Failure> onFailure);
        TrackedFile? DownloadContent(ContentId contentId, string fileLabel = "");
        TrackedFile? DownloadContent(ContentId contentId, Action<Failure> onFailure, string fileLabel = "");
        LocalDatasetList LocalFiles();
        CodexSpace Space();
        void ConnectToPeer(ICodexNode node);
        DebugInfoVersion Version { get; }
        IMarketplaceAccess Marketplace { get; }
        CrashWatcher CrashWatcher { get; }
        PodInfo GetPodInfo();
        ITransferSpeeds TransferSpeeds { get; }
        EthAccount EthAccount { get; }

        /// <summary>
        /// Warning! The node is not usable after this.
        /// TODO: Replace with delete-blocks debug call once available in Codex.
        /// </summary>
        void DeleteRepoFolder();
        void Stop(bool waitTillStopped);
    }

    public class CodexNode : ICodexNode
    {
        private const string UploadFailedMessage = "Unable to store block";
        private readonly IPluginTools tools;
        private readonly EthAccount? ethAccount;
        private readonly TransferSpeeds transferSpeeds;

        public CodexNode(IPluginTools tools, CodexAccess codexAccess, CodexNodeGroup group, IMarketplaceAccess marketplaceAccess, EthAccount? ethAccount)
        {
            this.tools = tools;
            this.ethAccount = ethAccount;
            CodexAccess = codexAccess;
            Group = group;
            Marketplace = marketplaceAccess;
            Version = new DebugInfoVersion();
            transferSpeeds = new TransferSpeeds();
        }

        public RunningPod Pod { get { return CodexAccess.Container; } }
        
        public RunningContainer Container { get { return Pod.Containers.Single(); } }
        public CodexAccess CodexAccess { get; }
        public CrashWatcher CrashWatcher { get => CodexAccess.CrashWatcher; }
        public CodexNodeGroup Group { get; }
        public IMarketplaceAccess Marketplace { get; }
        public DebugInfoVersion Version { get; private set; }
        public ITransferSpeeds TransferSpeeds { get => transferSpeeds; }

        public IMetricsScrapeTarget MetricsScrapeTarget
        {
            get
            {
                return new MetricsScrapeTarget(CodexAccess.Container.Containers.First(), CodexContainerRecipe.MetricsPortTag);
            }
        }

        public EthAddress EthAddress 
        {
            get
            {
                EnsureMarketplace();
                return ethAccount!.EthAddress;
            }
        }

        public EthAccount EthAccount
        {
            get
            {
                EnsureMarketplace();
                return ethAccount!;
            }
        }

        public string GetName()
        {
            return Container.Name;
        }

        public DebugInfo GetDebugInfo()
        {
            var debugInfo = CodexAccess.GetDebugInfo();
            var known = string.Join(",", debugInfo.Table.Nodes.Select(n => n.PeerId));
            Log($"Got DebugInfo with id: {debugInfo.Id}. This node knows: [{known}]");
            return debugInfo;
        }

        public DebugPeer GetDebugPeer(string peerId)
        {
            return CodexAccess.GetDebugPeer(peerId);
        }

        public ContentId UploadFile(TrackedFile file)
        {
            return UploadFile(file, DoNothing);
        }

        public ContentId UploadFile(TrackedFile file, Action<Failure> onFailure)
        {
            CodexAccess.LogDiskSpace("Before upload");

            using var fileStream = File.OpenRead(file.Filename);

            var logMessage = $"Uploading file {file.Describe()}...";
            Log(logMessage);
            var measurement = Stopwatch.Measure(tools.GetLog(), logMessage, () =>
            {
                return CodexAccess.UploadFile(fileStream, onFailure);
            });

            var response = measurement.Value;
            transferSpeeds.AddUploadSample(file.GetFilesize(), measurement.Duration);

            if (string.IsNullOrEmpty(response)) FrameworkAssert.Fail("Received empty response.");
            if (response.StartsWith(UploadFailedMessage)) FrameworkAssert.Fail("Node failed to store block.");

            Log($"Uploaded file. Received contentId: '{response}'.");
            CodexAccess.LogDiskSpace("After upload");

            return new ContentId(response);
        }

        public TrackedFile? DownloadContent(ContentId contentId, string fileLabel = "")
        {
            return DownloadContent(contentId, DoNothing, fileLabel);
        }

        public TrackedFile? DownloadContent(ContentId contentId, Action<Failure> onFailure, string fileLabel = "")
        {
            var logMessage = $"Downloading for contentId: '{contentId.Id}'...";
            Log(logMessage);
            var file = tools.GetFileManager().CreateEmptyFile(fileLabel);
            var measurement = Stopwatch.Measure(tools.GetLog(), logMessage, () => DownloadToFile(contentId.Id, file, onFailure));
            transferSpeeds.AddDownloadSample(file.GetFilesize(), measurement);
            Log($"Downloaded file {file.Describe()} to '{file.Filename}'.");
            return file;
        }

        public LocalDatasetList LocalFiles()
        {
            return CodexAccess.LocalFiles();
        }

        public CodexSpace Space()
        {
            return CodexAccess.Space();
        }

        public void ConnectToPeer(ICodexNode node)
        {
            var peer = (CodexNode)node;

            Log($"Connecting to peer {peer.GetName()}...");
            var peerInfo = node.GetDebugInfo();
            CodexAccess.ConnectToPeer(peerInfo.Id, GetPeerMultiAddresses(peer, peerInfo));

            Log($"Successfully connected to peer {peer.GetName()}.");
        }

        public PodInfo GetPodInfo()
        {
            return CodexAccess.GetPodInfo();
        }

        public void DeleteRepoFolder()
        {
            CodexAccess.DeleteRepoFolder();
        }

        public void Stop(bool waitTillStopped)
        {
            Log("Stopping...");
            CrashWatcher.Stop();
            Group.Stop(this, waitTillStopped);
        }

        public void EnsureOnlineGetVersionResponse()
        {
            var debugInfo = Time.Retry(CodexAccess.GetDebugInfo, "ensure online");
            var nodePeerId = debugInfo.Id;
            var nodeName = CodexAccess.Container.Name;

            if (!debugInfo.Version.IsValid())
            {
                throw new Exception($"Invalid version information received from Codex node {GetName()}: {debugInfo.Version}");
            }

            var log = tools.GetLog();
            log.AddStringReplace(nodePeerId, nodeName);
            log.AddStringReplace(debugInfo.Table.LocalNode.NodeId, nodeName);
            Version = debugInfo.Version;
        }

        private string[] GetPeerMultiAddresses(CodexNode peer, DebugInfo peerInfo)
        {
            // The peer we want to connect is in a different pod.
            // We must replace the default IP with the pod IP in the multiAddress.
            var workflow = tools.CreateWorkflow();
            var podInfo = workflow.GetPodInfo(peer.Pod);

            return peerInfo.Addrs.Select(a => a
                .Replace("0.0.0.0", podInfo.Ip))
                .ToArray();
        }

        private void DownloadToFile(string contentId, TrackedFile file, Action<Failure> onFailure)
        {
            CodexAccess.LogDiskSpace("Before download");

            using var fileStream = File.OpenWrite(file.Filename);
            try
            {
                using var downloadStream = CodexAccess.DownloadFile(contentId, onFailure);
                downloadStream.CopyTo(fileStream);
            }
            catch
            {
                Log($"Failed to download file '{contentId}'.");
                throw;
            }

            CodexAccess.LogDiskSpace("After download");
        }

        private void EnsureMarketplace()
        {
            if (ethAccount == null) throw new Exception("Marketplace is not enabled for this Codex node. Please start it with the option '.EnableMarketplace(...)' to enable it.");
        }

        private void Log(string msg)
        {
            tools.GetLog().Log($"{GetName()}: {msg}");
        }

        private void DoNothing(Failure failure)
        {
        }
    }
}
