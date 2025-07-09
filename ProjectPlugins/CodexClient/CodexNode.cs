using CodexClient.Hooks;
using FileUtils;
using Logging;
using Utils;

namespace CodexClient
{
    public partial interface ICodexNode : IHasEthAddress, IHasMetricsScrapeTarget
    {
        string GetName();
        string GetImageName();
        string GetPeerId();
        DebugInfo GetDebugInfo(bool log = false);
        void SetLogLevel(string logLevel);
        string GetSpr();
        DebugPeer GetDebugPeer(string peerId);
        ContentId UploadFile(TrackedFile file);
        ContentId UploadFile(TrackedFile file, string contentType, string contentDisposition);
        TrackedFile? DownloadContent(ContentId contentId, string fileLabel = "");
        TrackedFile? DownloadContent(ContentId contentId, TimeSpan timeout, string fileLabel = "");
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
        ITransferSpeeds TransferSpeeds { get; }
        EthAccount EthAccount { get; }
        StoragePurchase? GetPurchaseStatus(string purchaseId);

        Address GetDiscoveryEndpoint();
        Address GetApiEndpoint();
        Address GetListenEndpoint();

        /// <summary>
        /// Warning! The node is not usable after this.
        /// TODO: Replace with delete-blocks debug call once available in Codex.
        /// </summary>
        void DeleteDataDirFolder();
        void Stop(bool waitTillStopped);
        IDownloadedLog DownloadLog(string additionalName = "");
        bool HasCrashed();
    }

    public class CodexNode : ICodexNode
    {
        private const string UploadFailedMessage = "Unable to store block";
        private readonly ILog log;
        private readonly ICodexNodeHooks hooks;
        private readonly TransferSpeeds transferSpeeds;
        private string peerId = string.Empty;
        private string nodeId = string.Empty;
        private readonly CodexAccess codexAccess;
        private readonly IFileManager fileManager;

        public CodexNode(ILog log, CodexAccess codexAccess, IFileManager fileManager, IMarketplaceAccess marketplaceAccess, ICodexNodeHooks hooks)
        {
            this.codexAccess = codexAccess;
            this.fileManager = fileManager;
            Marketplace = marketplaceAccess;
            this.hooks = hooks;
            Version = new DebugInfoVersion();
            transferSpeeds = new TransferSpeeds();

            this.log = new LogPrefixer(log, $"{GetName()} ");
        }

        public void Awake()
        {
            hooks.OnNodeStarting(codexAccess.GetStartUtc(), codexAccess.GetImageName(), codexAccess.GetEthAccount());
        }

        public void Initialize()
        {
            // This is the moment we first connect to a codex node. Sometimes, Kubernetes takes a while to spin up the
            // container. So we'll adding a custom, generous retry here.
            var kubeSpinupRetry = new Retry("CodexNode_Initialize",
                maxTimeout: TimeSpan.FromMinutes(10.0),
                sleepAfterFail: TimeSpan.FromSeconds(10.0),
                onFail: f => { },
                failFast: false);

            kubeSpinupRetry.Run(InitializePeerNodeId);

            InitializeLogReplacements();

            hooks.OnNodeStarted(this, peerId, nodeId);
        }

        public IMarketplaceAccess Marketplace { get; }
        public DebugInfoVersion Version { get; private set; }
        public ITransferSpeeds TransferSpeeds { get => transferSpeeds; }

        public StoragePurchase? GetPurchaseStatus(string purchaseId)
        {
            return codexAccess.GetPurchaseStatus(purchaseId);
        }

        public EthAddress EthAddress 
        {
            get
            {
                EnsureMarketplace();
                return codexAccess.GetEthAccount()!.EthAddress;
            }
        }

        public EthAccount EthAccount
        {
            get
            {
                EnsureMarketplace();
                return codexAccess.GetEthAccount()!;
            }
        }

        public string GetName()
        {
            return codexAccess.GetName();
        }

        public string GetImageName()
        {
            return codexAccess.GetImageName();
        }

        public string GetPeerId()
        {
            return peerId;
        }

        public DebugInfo GetDebugInfo(bool log = false)
        {
            var debugInfo = codexAccess.GetDebugInfo();
            if (log)
            {
                var known = string.Join(",", debugInfo.Table.Nodes.Select(n => n.PeerId));
                Log($"Got DebugInfo with id: {debugInfo.Id}. This node knows: [{known}]");
            }
            return debugInfo;
        }

        public void SetLogLevel(string logLevel)
        {
            codexAccess.SetLogLevel(logLevel);
        }

        public string GetSpr()
        {
            return codexAccess.GetSpr();
        }

        public DebugPeer GetDebugPeer(string peerId)
        {
            return codexAccess.GetDebugPeer(peerId);
        }

        public ContentId UploadFile(TrackedFile file)
        {
            return UploadFile(file, "application/octet-stream", $"attachment; filename=\"{Path.GetFileName(file.Filename)}\"");
        }

        public ContentId UploadFile(TrackedFile file, string contentType, string contentDisposition)
        {
            using var fileStream = File.OpenRead(file.Filename);
            var uniqueId = Guid.NewGuid().ToString();
            var size = file.GetFilesize();

            hooks.OnFileUploading(uniqueId, size);

            var input = new UploadInput(contentType, contentDisposition, fileStream);
            var logMessage = $"Uploading file {file.Describe()} with contentType: '{input.ContentType}' and disposition: '{input.ContentDisposition}'...";
            var measurement = Stopwatch.Measure(log, logMessage, () =>
            {
                return codexAccess.UploadFile(input);
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
            return DownloadContent(contentId, TimeSpan.FromMinutes(10.0), fileLabel);
        }

        public TrackedFile? DownloadContent(ContentId contentId, TimeSpan timeout, string fileLabel = "")
        {
            var file = fileManager.CreateEmptyFile(fileLabel);
            hooks.OnFileDownloading(contentId);
            Log($"Downloading '{contentId}'...");

            var logMessage = $"Downloaded '{contentId}' to '{file.Filename}'";
            var measurement = Stopwatch.Measure(log, logMessage, () => DownloadToFile(contentId.Id, file, timeout));

            var size = file.GetFilesize();
            transferSpeeds.AddDownloadSample(size, measurement);
            hooks.OnFileDownloaded(size, contentId);

            return file;
        }

        public LocalDataset DownloadStreamless(ContentId cid)
        {
            Log($"Downloading streamless '{cid}' (no-wait)");
            return codexAccess.DownloadStreamless(cid);
        }

        public LocalDataset DownloadStreamlessWait(ContentId cid, ByteSize size)
        {
            Log($"Downloading streamless '{cid}' (wait till finished)");

            var sw = Stopwatch.Measure(log, nameof(DownloadStreamlessWait), () =>
            {
                var startSpace = Space();
                var result = codexAccess.DownloadStreamless(cid);
                WaitUntilQuotaUsedIncreased(startSpace, size);
                return result;
            });

            return sw.Value;
        }

        public LocalDataset DownloadManifestOnly(ContentId cid)
        {
            Log($"Downloading manifest-only '{cid}'");
            return codexAccess.DownloadManifestOnly(cid);
        }

        public LocalDatasetList LocalFiles()
        {
            return codexAccess.LocalFiles();
        }

        public CodexSpace Space()
        {
            return codexAccess.Space();
        }

        public void ConnectToPeer(ICodexNode node)
        {
            var peer = (CodexNode)node;

            Log($"Connecting to peer {peer.GetName()}...");
            var peerInfo = node.GetDebugInfo();
            codexAccess.ConnectToPeer(peerInfo.Id, GetPeerMultiAddresses(peer, peerInfo));

            Log($"Successfully connected to peer {peer.GetName()}.");
        }

        public void DeleteDataDirFolder()
        {
            codexAccess.DeleteDataDirFolder();
        }

        public void Stop(bool waitTillStopped)
        {
            Log("Stopping...");
            hooks.OnNodeStopping();
            codexAccess.Stop(waitTillStopped);
        }

        public IDownloadedLog DownloadLog(string additionalName = "")
        {
            return codexAccess.DownloadLog(additionalName);
        }

        public Address GetDiscoveryEndpoint()
        {
            return codexAccess.GetDiscoveryEndpoint();
        }

        public Address GetApiEndpoint()
        {
            return codexAccess.GetApiEndpoint();
        }

        public Address GetListenEndpoint()
        {
            return codexAccess.GetListenEndpoint();
        }

        public Address GetMetricsScrapeTarget()
        {
            var address = codexAccess.GetMetricsEndpoint();
            if (address == null) throw new Exception("Metrics ScrapeTarget accessed, but node was not started with EnableMetrics()");
            return address;
        }

        public bool HasCrashed()
        {
            return codexAccess.HasCrashed();
        }

        public override string ToString()
        {
            return $"CodexNode:{GetName()}";
        }

        private void InitializePeerNodeId()
        {
            var debugInfo = codexAccess.GetDebugInfo();
            if (!debugInfo.Version.IsValid())
            {
                throw new Exception($"Invalid version information received from Codex node {GetName()}: {debugInfo.Version}");
            }

            peerId = debugInfo.Id;
            nodeId = debugInfo.Table.LocalNode.NodeId;
            Version = debugInfo.Version;
        }

        private void InitializeLogReplacements()
        {
            var nodeName = GetName();

            log.AddStringReplace(peerId, nodeName);
            log.AddStringReplace(CodexUtils.ToShortId(peerId), nodeName);
            log.AddStringReplace(nodeId, nodeName);
            log.AddStringReplace(CodexUtils.ToShortId(nodeId), nodeName);

            var ethAccount = codexAccess.GetEthAccount();
            if (ethAccount != null)
            {
                var addr = ethAccount.EthAddress.ToString();
                log.AddStringReplace(addr, nodeName);
                log.AddStringReplace(addr.ToLowerInvariant(), nodeName);
            }
        }

        private string[] GetPeerMultiAddresses(CodexNode peer, DebugInfo peerInfo)
        {
            var peerId = peer.GetDiscoveryEndpoint().Host
                .Replace("http://", "")
                .Replace("https://", "");

            return peerInfo.Addrs.Select(a => a
                .Replace("0.0.0.0", peerId))
                .ToArray();
        }

        private void DownloadToFile(string contentId, TrackedFile file, TimeSpan timeout)
        {
            using var fileStream = File.OpenWrite(file.Filename);
            try
            {
                // Type of stream generated by openAPI client does not support timeouts.
                // So we use a task and cancellation token to track our timeout manually.

                var start = DateTime.UtcNow;
                var cts = new CancellationTokenSource();
                var downloadTask = Task.Run(() =>
                {
                    using var downloadStream = codexAccess.DownloadFile(contentId);
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
            catch (Exception ex)
            {
                Log($"Failed to download file '{contentId}': {ex}");
                throw;
            }
        }

        public void WaitUntilQuotaUsedIncreased(CodexSpace startSpace, ByteSize expectedIncreaseOfQuotaUsed)
        {
            WaitUntilQuotaUsedIncreased(startSpace, expectedIncreaseOfQuotaUsed, TimeSpan.FromMinutes(10));
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
            sleepAfterFail: TimeSpan.FromSeconds(10),
            onFail: f => { },
            failFast: false);

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
            if (codexAccess.GetEthAccount() == null) throw new Exception("Marketplace is not enabled for this Codex node. Please start it with the option '.EnableMarketplace(...)' to enable it.");
        }

        private void Log(string msg)
        {
            log.Log(msg);
        }
    }
}
