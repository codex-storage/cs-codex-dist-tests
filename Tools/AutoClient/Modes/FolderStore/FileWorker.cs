using CodexClient;
using Logging;

namespace AutoClient.Modes.FolderStore
{
    public interface IWorkEventHandler
    {
        void OnFileUploaded();
        void OnNewPurchase();
        void OnPurchaseExpired();
        void OnPurchaseStarted();
    }

    public class FileWorker : FileStatus
    {
        private readonly App app;
        private readonly CodexWrapper node;
        private readonly ILog log;
        private readonly PurchaseInfo purchaseInfo;
        private readonly string sourceFilename;
        private readonly IWorkEventHandler eventHandler;

        public FileWorker(App app, CodexWrapper node, PurchaseInfo purchaseInfo, string folder, FileIndex fileIndex, IWorkEventHandler eventHandler)
            : base(app, folder, fileIndex.File + ".json", purchaseInfo)
        {
            this.app = app;
            this.node = node;
            log = new LogPrefixer(app.Log, GetFileTag(fileIndex));
            this.purchaseInfo = purchaseInfo;
            sourceFilename = fileIndex.File;
            if (sourceFilename.ToLowerInvariant().EndsWith(".json")) throw new Exception("Not an era file.");
            this.eventHandler = eventHandler;
        }

        protected override void OnNewState(WorkerStatus newState)
        {
            newState.LastUpdate = DateTime.MinValue;
        }

        public void Update()
        {
            try
            {
                if (IsCurrentlyRunning() && UpdatedRecently()) return;

                Log($"Updating for '{sourceFilename}'...");
                EnsureRecentPurchase();
                SaveState();
                app.Log.Log("");
            }
            catch (Exception exc)
            {
                app.Log.Error("Exception during fileworker update: " + exc);
                State.Error = exc.ToString();
                SaveState();
                throw;
            }
        }

        private bool UpdatedRecently()
        {
            var now = DateTime.UtcNow;
            return State.LastUpdate + TimeSpan.FromMinutes(15) > now;
        }

        private string EnsureCid()
        {
            Log($"Checking CID...");

            if (!string.IsNullOrEmpty(State.EncodedCid) &&
                DoesCidExistInNetwork(State.EncodedCid))
            {
                Log("Encoded-CID successfully found in the network.");
                // TODO: Using the encoded CID currently would result in double-encoding of the dataset.
                // See: https://github.com/codex-storage/nim-codex/issues/1005
                // Always use the basic CID for now, even though we have to repeat the encoding.
                // When using encoded CID works: return State.EncodedCid;
            }

            if (!string.IsNullOrEmpty(State.Cid) &&
                DoesCidExistInNetwork(State.Cid))
            {
                Log("Basic-CID successfully found in the network.");
                return State.Cid;
            }

            if (string.IsNullOrEmpty(State.Cid))
            {
                Log("File was not previously uploaded.");
            }

            Log($"Uploading...");
            var cid = node.UploadFile(sourceFilename);
            eventHandler.OnFileUploaded();
            Log("Got Basic-CID: " + cid);
            State.Cid = cid.Id;
            SaveState();
            return State.Cid;
        }

        private bool DoesCidExistInNetwork(string cid)
        {
            try
            {
                // This should not take longer than a few seconds. If it does, cancel it.
                var cts = new CancellationTokenSource();
                var cancelTask = Task.Run(() =>
                {
                    Thread.Sleep(TimeSpan.FromSeconds(15));
                    cts.Cancel();
                });

                var manifest = node.Node.DownloadManifestOnly(new ContentId(cid));
                if (manifest == null) return false;
            }
            catch
            {
                return false;
            }
            return true;
        }

        private void EnsureRecentPurchase()
        {
            Log($"Checking recent purchase...");
            var recent = GetMostRecent();
            if (recent == null)
            {
                Log($"No recent purchase.");
                MakeNewPurchase();
                return;
            }

            UpdatePurchase(recent);

            if (recent.Expiry.HasValue)
            {
                Log($"Purchase has failed or expired.");
                MakeNewPurchase();
                eventHandler.OnPurchaseExpired();
                return;
            }

            if (recent.Finish.HasValue)
            {
                Log($"Purchase has finished.");
                MakeNewPurchase();
                return;
            }

            var safeEnd = recent.Created + purchaseInfo.PurchaseDurationSafe;
            if (recent.Started.HasValue && DateTime.UtcNow > safeEnd)
            {
                Log($"Purchase is going to expire soon.");
                MakeNewPurchase();
                return;
            }

            if (!recent.Submitted.HasValue)
            {
                Log($"Purchase is waiting to be submitted.");
                return;
            }

            if (recent.Submitted.HasValue && !recent.Started.HasValue)
            {
                Log($"Purchase is submitted and waiting to start.");
                return;
            }

            Log($"Purchase is running.");
        }

        private void UpdatePurchase(WorkerPurchase recent)
        {
            if (string.IsNullOrEmpty(recent.Pid)) throw new Exception("No purchaseID!");
            var now = DateTime.UtcNow;

            var purchase = node.GetStoragePurchase(recent.Pid);
            if (purchase == null)
            {
                Log($"No purchase information found for PID '{recent.Pid}'. Consider this one expired.");
                recent.Expiry = now;
                return;
            }

            if (purchase.IsSubmitted)
            {
                if (!recent.Submitted.HasValue) recent.Submitted = now;
            }
            if (purchase.IsStarted)
            {
                if (!recent.Submitted.HasValue) recent.Submitted = now;
                if (!recent.Started.HasValue)
                {
                    Log($"Detected new purchase-start for '{recent.Pid}'.");
                    recent.Started = now;
                    eventHandler.OnPurchaseStarted();
                }
            }
            if (purchase.IsCancelled)
            {
                if (!recent.Submitted.HasValue) recent.Submitted = now;
                if (!recent.Expiry.HasValue) recent.Expiry = now;
            }
            if (purchase.IsError)
            {
                if (!recent.Submitted.HasValue) recent.Submitted = now;
                if (!recent.Expiry.HasValue) recent.Expiry = now;
            }
            if (purchase.IsFinished)
            {
                if (!recent.Submitted.HasValue) recent.Submitted = now;
                if (!recent.Started.HasValue) recent.Started = now;
                if (!recent.Finish.HasValue) recent.Finish = now;
            }
            State.LastUpdate = now;
            SaveState();
        }

        private void MakeNewPurchase()
        {
            var cid = EnsureCid();
            if (string.IsNullOrEmpty(cid)) throw new Exception("No cid!");

            Log($"Creating new purchase...");
            var response = node.RequestStorage(new ContentId(cid));
            var purchaseId = response.PurchaseId;
            var encodedCid = response.ContentId;
            if (string.IsNullOrEmpty(purchaseId) ||
                purchaseId == "Unable to encode manifest" ||
                purchaseId == "Purchasing not available" ||
                purchaseId == "Expiry required" ||
                purchaseId == "Expiry needs to be in future" ||
                purchaseId == "Expiry has to be before the request's end (now + duration)")
            {
                throw new InvalidOperationException(purchaseId);
            }

            var newPurchase = new WorkerPurchase
            {
                Created = DateTime.UtcNow,
                Pid = purchaseId
            };
            State.Purchases = State.Purchases.Concat([newPurchase]).ToArray();
            State.EncodedCid = encodedCid.Id;
            SaveState();
            eventHandler.OnNewPurchase();

            Log($"New purchase created. PID: '{purchaseId}'.");
            Log("Got Encoded-CID: " + encodedCid);
            Log("Waiting for submit...");
            Thread.Sleep(500);

            var timeout = DateTime.UtcNow + TimeSpan.FromMinutes(5);
            while (DateTime.UtcNow < timeout)
            {
                Thread.Sleep(5000);
                UpdatePurchase(newPurchase);
                if (newPurchase.Submitted.HasValue)
                {
                    Log("New purchase successfully submitted.");
                    return;
                }
            }
            Log("New purchase was not submitted within 5-minute timeout. Will check again later...");
        }

        private void Log(string msg)
        {
            log.Log(msg);
        }

        private string GetFileTag(FileIndex filename)
        {
            return $"({filename.Index.ToString("00000")}) ";
        }

        [Serializable]
        public class WorkerStatus
        {
            public DateTime LastUpdate { get; set; }
            public string Cid { get; set; } = string.Empty;
            public string EncodedCid { get; set; } = string.Empty;
            public string Error { get; set; } = string.Empty;
            public WorkerPurchase[] Purchases { get; set; } = Array.Empty<WorkerPurchase>();
        }

        [Serializable]
        public class WorkerPurchase
        {
            public string Pid { get; set; } = string.Empty;
            public DateTime Created { get; set; }
            public DateTime? Submitted { get; set; }
            public DateTime? Started { get; set; }
            public DateTime? Expiry { get; set; }
            public DateTime? Finish { get; set; }
        }
    }
}
