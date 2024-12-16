using Logging;

namespace AutoClient.Modes.FolderStore
{
    public class FileWorker : FileStatus
    {
        private readonly App app;
        private readonly ILog log;
        private readonly ICodexInstance instance;
        private readonly PurchaseInfo purchaseInfo;
        private readonly string sourceFilename;
        private readonly Action onFileUploaded;
        private readonly Action onNewPurchase;
        private readonly CodexNode codex;

        public FileWorker(App app, ICodexInstance instance, PurchaseInfo purchaseInfo, string folder, FileIndex fileIndex, Action onFileUploaded, Action onNewPurchase)
            : base(app, folder, fileIndex.File + ".json", purchaseInfo)
        {
            this.app = app;
            log = new LogPrefixer(app.Log, GetFileTag(fileIndex));
            this.instance = instance;
            this.purchaseInfo = purchaseInfo;
            sourceFilename = fileIndex.File;
            if (sourceFilename.ToLowerInvariant().EndsWith(".json")) throw new Exception("Not an era file.");
            this.onFileUploaded = onFileUploaded;
            this.onNewPurchase = onNewPurchase;
            codex = new CodexNode(app, instance);
        }

        public int FailureCounter => State.FailureCounter;

        protected override void OnNewState(WorkerStatus newState)
        {
            newState.LastUpdate = DateTime.MinValue;
        }

        public async Task Update()
        {
            try
            {
                if (IsCurrentlyRunning() && UpdatedRecently()) return;

                Log($"Updating for '{sourceFilename}'...");
                await EnsureRecentPurchase();
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

        private async Task<string> EnsureCid()
        {
            Log($"Checking CID...");

            if (!string.IsNullOrEmpty(State.EncodedCid) &&
                await DoesCidExistInNetwork(State.EncodedCid))
            {
                Log("Encoded-CID successfully found in the network.");
                // TODO: Using the encoded CID currently would result in double-encoding of the dataset.
                // See: https://github.com/codex-storage/nim-codex/issues/1005
                // Always use the basic CID for now, even though we have to repeat the encoding.
                // When using encoded CID works: return State.EncodedCid;
            }

            if (!string.IsNullOrEmpty(State.Cid) &&
                await DoesCidExistInNetwork(State.Cid))
            {
                Log("Basic-CID successfully found in the network.");
                return State.Cid;
            }

            if (string.IsNullOrEmpty(State.Cid))
            {
                Log("File was not previously uploaded.");
            }

            Log($"Uploading...");
            var cid = await codex.UploadFile(sourceFilename);
            onFileUploaded();
            Log("Got Basic-CID: " + cid);
            State.Cid = cid.Id;
            SaveState();
            return State.Cid;
        }

        private async Task<bool> DoesCidExistInNetwork(string cid)
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

                var manifest = await instance.Codex.DownloadNetworkManifestAsync(cid, cts.Token);
                if (manifest == null) return false;
            }
            catch
            {
                return false;
            }
            return true;
        }

        private async Task EnsureRecentPurchase()
        {
            Log($"Checking recent purchase...");
            var recent = GetMostRecent();
            if (recent == null)
            {
                Log($"No recent purchase.");
                await MakeNewPurchase();
                return;
            }

            await UpdatePurchase(recent);

            if (recent.Expiry.HasValue)
            {
                Log($"Purchase has failed or expired.");
                await MakeNewPurchase();
                State.FailureCounter++;
                return;
            }

            if (recent.Finish.HasValue)
            {
                Log($"Purchase has finished.");
                await MakeNewPurchase();
                return;
            }

            var safeEnd = recent.Created + purchaseInfo.PurchaseDurationSafe;
            if (recent.Started.HasValue && DateTime.UtcNow > safeEnd)
            {
                Log($"Purchase is going to expire soon.");
                await MakeNewPurchase();
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

        private async Task UpdatePurchase(WorkerPurchase recent)
        {
            if (string.IsNullOrEmpty(recent.Pid)) throw new Exception("No purchaseID!");
            var now = DateTime.UtcNow;

            var purchase = await codex.GetStoragePurchase(recent.Pid);
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
                if (!recent.Started.HasValue) recent.Started = now;
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

        private async Task MakeNewPurchase()
        {
            var cid = await EnsureCid();
            if (string.IsNullOrEmpty(cid)) throw new Exception("No cid!");

            Log($"Creating new purchase...");
            var response = await codex.RequestStorage(new CodexPlugin.ContentId(cid));
            var purchaseId = response.PurchaseId;
            var encodedCid = response.EncodedCid;
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
            onNewPurchase();

            Log($"New purchase created. PID: '{purchaseId}'.");
            Log("Got Encoded-CID: " + encodedCid);
            Log("Waiting for submit...");
            Thread.Sleep(500);

            var timeout = DateTime.UtcNow + TimeSpan.FromMinutes(5);
            while (DateTime.UtcNow < timeout)
            {
                Thread.Sleep(5000);
                await UpdatePurchase(newPurchase);
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
            public int FailureCounter { get; set; } = 0;
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
