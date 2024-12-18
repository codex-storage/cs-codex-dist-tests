using CodexPlugin.Hooks;
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
        string GetPeerId();
        DebugInfo GetDebugInfo(bool log = false);
        string GetSpr();
        DebugPeer GetDebugPeer(string peerId);
        ContentId UploadFile(TrackedFile file);
        ContentId UploadFile(TrackedFile file, Action<Failure> onFailure);
        ContentId UploadFile(TrackedFile file, string contentType, string contentDisposition, Action<Failure> onFailure);
        TrackedFile? DownloadContent(ContentId contentId, string fileLabel = "");
        TrackedFile? DownloadContent(ContentId contentId, Action<Failure> onFailure, string fileLabel = "");
        LocalDataset DownloadStreamless(ContentId cid);
        /// <summary>
        /// TODO: This will monitor the quota-used of the node until 'size' bytes are added. That's a very bad way
        /// to track the streamless download progress. Replace it once we have a good API for this.
        /// </summary>
        LocalDataset DownloadStreamlessWait(ContentId cid, ByteSize size);
        LocalDataset DownloadManifestOnly(ContentId cid);
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
        private readonly ILog log;
        private readonly IPluginTools tools;
        private readonly ICodexNodeHooks hooks;
        private readonly EthAccount? ethAccount;
        private readonly TransferSpeeds transferSpeeds;
        private string peerId = string.Empty;
        private string nodeId = string.Empty;

        public CodexNode(IPluginTools tools, CodexAccess codexAccess, CodexNodeGroup group, IMarketplaceAccess marketplaceAccess, ICodexNodeHooks hooks, EthAccount? ethAccount)
        {
            this.tools = tools;
            this.ethAccount = ethAccount;
            CodexAccess = codexAccess;
            Group = group;
            Marketplace = marketplaceAccess;
            this.hooks = hooks;
            Version = new DebugInfoVersion();
            transferSpeeds = new TransferSpeeds();

            log = new LogPrefixer(tools.GetLog(), $"{GetName()} ");
        }

        public void Awake()
        {
            hooks.OnNodeStarting(Container.Recipe.RecipeCreatedUtc, Container.Recipe.Image, ethAccount);
        }

        public void Initialize()
        {
            hooks.OnNodeStarted(peerId, nodeId);
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

        public string GetPeerId()
        {
            return peerId;
        }

        public DebugInfo GetDebugInfo(bool log = false)
        {
            var debugInfo = CodexAccess.GetDebugInfo();
            if (log)
            {
                var known = string.Join(",", debugInfo.Table.Nodes.Select(n => n.PeerId));
                Log($"Got DebugInfo with id: {debugInfo.Id}. This node knows: [{known}]");
            }
            return debugInfo;
        }

        public string GetSpr()
        {
            return CodexAccess.GetSpr();
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
            return UploadFile(file, "application/octet-stream", $"attachment; filename=\"{Path.GetFileName(file.Filename)}\"", onFailure);
        }

        public ContentId UploadFile(TrackedFile file, string contentType, string contentDisposition, Action<Failure> onFailure)
        {
            using var fileStream = File.OpenRead(file.Filename);
            var uniqueId = Guid.NewGuid().ToString();
            var size = file.GetFilesize();

            hooks.OnFileUploading(uniqueId, size);

            var input = new UploadInput(contentType, contentDisposition, fileStream);
            var logMessage = $"Uploading file {file.Describe()} with contentType: '{input.ContentType}' and disposition: '{input.ContentDisposition}'...";
            var measurement = Stopwatch.Measure(log, logMessage, () =>
            {
                return CodexAccess.UploadFile(input, onFailure);
            });

            var response = measurement.Value;
            transferSpeeds.AddUploadSample(size, measurement.Duration);

            if (string.IsNullOrEmpty(response)) FrameworkAssert.Fail("Received empty response.");
            if (response.StartsWith(UploadFailedMessage)) FrameworkAssert.Fail("Node failed to store block.");

            Log($"Uploaded file {file.Describe()}. Received contentId: '{response}'.");

            var cid = new ContentId(response);
            hooks.OnFileUploaded(uniqueId, size, cid);
            return cid;
        }

        public TrackedFile? DownloadContent(ContentId contentId, string fileLabel = "")
        {
            return DownloadContent(contentId, DoNothing, fileLabel);
        }

        public TrackedFile? DownloadContent(ContentId contentId, Action<Failure> onFailure, string fileLabel = "")
        {
            var file = tools.GetFileManager().CreateEmptyFile(fileLabel);
            hooks.OnFileDownloading(contentId);
            Log($"Downloading '{contentId}'...");

            var logMessage = $"Downloaded '{contentId}' to '{file.Filename}'";
            var measurement = Stopwatch.Measure(log, logMessage, () => DownloadToFile(contentId.Id, file, onFailure));

            var size = file.GetFilesize();
            transferSpeeds.AddDownloadSample(size, measurement);
            hooks.OnFileDownloaded(size, contentId);

            return file;
        }

        public LocalDataset DownloadStreamless(ContentId cid)
        {
            Log($"Downloading streamless '{cid}' (no-wait)");
            return CodexAccess.DownloadStreamless(cid);
        }

        public LocalDataset DownloadStreamlessWait(ContentId cid, ByteSize size)
        {
            Log($"Downloading streamless '{cid}' (wait till finished)");

            var sw = Stopwatch.Measure(log, nameof(DownloadStreamlessWait), () =>
            {
                var startSpace = Space();
                var result = CodexAccess.DownloadStreamless(cid);
                WaitUntilQuotaUsedIncreased(startSpace, size);
                return result;
            });

            return sw.Value;
        }

        public LocalDataset DownloadManifestOnly(ContentId cid)
        {
            Log($"Downloading manifest-only '{cid}'");
            return CodexAccess.DownloadManifestOnly(cid);
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
            hooks.OnNodeStopping();

            CrashWatcher.Stop();
            Group.Stop(this, waitTillStopped);
        }

        public void EnsureOnlineGetVersionResponse()
        {
            var debugInfo = Time.Retry(CodexAccess.GetDebugInfo, "ensure online");
            peerId = debugInfo.Id;
            nodeId = debugInfo.Table.LocalNode.NodeId;
            var nodeName = CodexAccess.Container.Name;

            if (!debugInfo.Version.IsValid())
            {
                throw new Exception($"Invalid version information received from Codex node {GetName()}: {debugInfo.Version}");
            }

            log.AddStringReplace(peerId, nodeName);
            log.AddStringReplace(CodexUtils.ToShortId(peerId), nodeName);
            log.AddStringReplace(debugInfo.Table.LocalNode.NodeId, nodeName);
            log.AddStringReplace(CodexUtils.ToShortId(debugInfo.Table.LocalNode.NodeId), nodeName);
            Version = debugInfo.Version;
        }

        public override string ToString()
        {
            return $"CodexNode:{GetName()}";
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
            using var fileStream = File.OpenWrite(file.Filename);
            var timeout = tools.TimeSet.HttpCallTimeout();
            try
            {
                // Type of stream generated by openAPI client does not support timeouts.
                var start = DateTime.UtcNow;
                var cts = new CancellationTokenSource();
                var downloadTask = Task.Run(() =>
                {
                    using var downloadStream = CodexAccess.DownloadFile(contentId, onFailure);
                    downloadStream.CopyTo(fileStream);
                }, cts.Token);
                
                while (DateTime.UtcNow - start < timeout)
                {
                    if (downloadTask.IsFaulted) throw downloadTask.Exception;
                    if (downloadTask.IsCompletedSuccessfully) return;
                    Thread.Sleep(100);
                }

                cts.Cancel();
                throw new TimeoutException($"Download of '{contentId}' timed out after {Time.FormatDuration(timeout)}");
            }
            catch
            {
                Log($"Failed to download file '{contentId}'.");
                throw;
            }
        }

        public void WaitUntilQuotaUsedIncreased(CodexSpace startSpace, ByteSize expectedIncreaseOfQuotaUsed)
        {
            WaitUntilQuotaUsedIncreased(startSpace, expectedIncreaseOfQuotaUsed, TimeSpan.FromMinutes(2));
        }

        public void WaitUntilQuotaUsedIncreased(
            CodexSpace startSpace,
            ByteSize expectedIncreaseOfQuotaUsed,
            TimeSpan maxTimeout)
        {
            Log($"Waiting until quotaUsed " +
                $"(start: {startSpace.QuotaUsedBytes}) " +
                $"increases by {expectedIncreaseOfQuotaUsed} " +
                $"to reach {startSpace.QuotaUsedBytes + expectedIncreaseOfQuotaUsed.SizeInBytes}");

            var retry = new Retry($"Checking local space for quotaUsed increase of {expectedIncreaseOfQuotaUsed}",
            maxTimeout: maxTimeout,
            sleepAfterFail: TimeSpan.FromSeconds(3),
            onFail: f => { });

            retry.Run(() =>
            {
                var space = Space();
                var increase = space.QuotaUsedBytes - startSpace.QuotaUsedBytes;

                if (increase < expectedIncreaseOfQuotaUsed.SizeInBytes)
                    throw new Exception($"Expected quota-used not reached. " +
                        $"Expected increase: {expectedIncreaseOfQuotaUsed.SizeInBytes} " +
                        $"Actual increase: {increase} " +
                        $"Actual used: {space.QuotaUsedBytes}");
            });
        }

        private void EnsureMarketplace()
        {
            if (ethAccount == null) throw new Exception("Marketplace is not enabled for this Codex node. Please start it with the option '.EnableMarketplace(...)' to enable it.");
        }

        private void Log(string msg)
        {
            log.Log(msg);
        }

        private void DoNothing(Failure failure)
        {
        }
    }
}
