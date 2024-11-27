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

        public FileWorker(App app, ICodexInstance instance, PurchaseInfo purchaseInfo, string folder, FileIndex filename, Action onFileUploaded, Action onNewPurchase)
            : base(app, folder, filename + ".json", purchaseInfo)
        {
            this.app = app;
            log = new LogPrefixer(app.Log, GetFileTag(filename));
            this.instance = instance;
            this.purchaseInfo = purchaseInfo;
            sourceFilename = filename.File;
            if (sourceFilename.ToLowerInvariant().EndsWith(".json")) throw new Exception("Not an era file.");
            this.onFileUploaded = onFileUploaded;
            this.onNewPurchase = onNewPurchase;
            codex = new CodexNode(app, instance);
        }

        public int FailureCounter => State.FailureCounter;

        public async Task Update()
        {
            try
            {
                Log($"Updating for '{sourceFilename}'...");
                await EnsureCid();
                await EnsureRecentPurchase();
                SaveState();
                app.Log.Log("");
            }
            catch (Exception exc)
            {
                app.Log.Error("Exception during fileworker update: " + exc);
                throw;
            }
        }

        private async Task EnsureCid()
        {
            Log($"Ensuring CID...");
            if (!string.IsNullOrEmpty(State.Cid))
            {
                var found = true;
                try
                {
                    var manifest = await instance.Codex.DownloadNetworkManifestAsync(State.Cid);
                    if (manifest == null) found = false;
                }
                catch
                {
                    found = false;
                }

                if (!found)
                {
                    Log($"Existing CID '{State.Cid}' could not be found in the network.");
                    State.Cid = "";
                }
                else
                {
                    Log($"Existing CID '{State.Cid}' was successfully found in the network.");
                }
            }
            else
            {
                Log("File was not previously uploaded.");
            }

            if (string.IsNullOrEmpty(State.Cid))
            {
                Log($"Uploading...");
                var cid = await codex.UploadFile(sourceFilename);
                onFileUploaded();
                Log("Got CID: " + cid);
                State.Cid = cid.Id;
                SaveState();
                Thread.Sleep(1000);
            }
        }

        private async Task EnsureRecentPurchase()
        {
            Log($"Ensuring recent purchase...");
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
        }

        private async Task MakeNewPurchase()
        {
            if (string.IsNullOrEmpty(State.Cid)) throw new Exception("No cid!");

            Log($"Creating new purchase...");
            var response = await codex.RequestStorage(new CodexPlugin.ContentId(State.Cid));
            var purchaseId = response.PurchaseId;
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
            SaveState();

            Log($"New purchase created. PID: '{purchaseId}'. Waiting for submit...");
            Thread.Sleep(500);
            onNewPurchase();

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
            public string Cid { get; set; } = string.Empty;
            public int FailureCounter { get; set; } = 0;
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
