using static AutoClient.Modes.FolderStore.FileWorker;

namespace AutoClient.Modes.FolderStore
{
    public class FileWorker : JsonBacked<WorkerStatus>
    {
        private readonly App app;
        private readonly PurchaseInfo purchaseInfo;
        private readonly string sourceFilename;

        public FileWorker(App app, PurchaseInfo purchaseInfo, string folder, string filename)
            : base(app, folder, filename + ".json")
        {
            this.app = app;
            this.purchaseInfo = purchaseInfo;
            sourceFilename = filename;
        }

        public int FailureCounter => State.FailureCounter;

        public async Task Update(ICodexInstance instance, Action shouldRevisitSoon)
        {
            try
            {
                var codex = new CodexNode(app, instance);
                await EnsureCid(instance, codex);
                await EnsureRecentPurchase(instance, codex, shouldRevisitSoon);
                SaveState();
                app.Log.Log("");
            }
            catch (Exception exc)
            {
                app.Log.Error("Exception during fileworker update: " + exc);
                throw;
            }
        }

        private async Task EnsureRecentPurchase(ICodexInstance instance, CodexNode codex, Action shouldRevisitSoon)
        {
            app.Log.Log($"Ensuring recent purchase for '{sourceFilename}'...");
            var recent = GetMostRecent();
            if (recent == null)
            {
                app.Log.Log($"No recent purchase for '{sourceFilename}'.");
                await MakeNewPurchase(instance, codex);
                shouldRevisitSoon();
                return;
            }

            await UpdatePurchase(recent, instance, codex);

            if (recent.Expiry.HasValue)
            {
                app.Log.Log($"Purchase for '{sourceFilename}' has failed or expired.");
                await MakeNewPurchase(instance, codex);
                shouldRevisitSoon();
                State.FailureCounter++;
                return;
            }

            if (recent.Finish.HasValue)
            {
                app.Log.Log($"Purchase for '{sourceFilename}' has finished.");
                await MakeNewPurchase(instance, codex);
                shouldRevisitSoon();
                return;
            }

            if (recent.Started.HasValue &&
                recent.Created + purchaseInfo.PurchaseDurationSafe > DateTime.UtcNow)
            {
                app.Log.Log($"Purchase for '{sourceFilename}' is going to expire soon.");
                await MakeNewPurchase(instance, codex);
                shouldRevisitSoon();
                return;
            }

            if (!recent.Submitted.HasValue)
            {
                app.Log.Log($"Purchase for '{sourceFilename}' is waiting to be submitted.");
                shouldRevisitSoon();
                return;
            }

            if (recent.Submitted.HasValue && !recent.Started.HasValue)
            {
                app.Log.Log($"Purchase for '{sourceFilename}' is submitted and waiting to start.");
                shouldRevisitSoon();
                return;
            }

            app.Log.Log($"Purchase for '{sourceFilename}' is running.");
        }

        private async Task UpdatePurchase(WorkerPurchase recent, ICodexInstance instance, CodexNode codex)
        {
            if (string.IsNullOrEmpty(recent.Pid)) throw new Exception("No purchaseID!");
            var now = DateTime.UtcNow;

            var purchase = await codex.GetStoragePurchase(recent.Pid);
            if (purchase == null)
            {
                app.Log.Log($"No purchase information found for PID '{recent.Pid}' for file '{sourceFilename}'. Consider this one expired.");
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

            app.Log.Log($"Updated purchase information for PID '{recent.Pid}' for file '{sourceFilename}': " +
                $"Submitted: {recent.Submitted.HasValue} " +
                $"Started: {recent.Started.HasValue} " +
                $"Expiry: {recent.Expiry.HasValue} " +
                $"Finish: {recent.Finish.HasValue}");
        }

        private async Task MakeNewPurchase(ICodexInstance instance, CodexNode codex)
        {
            if (string.IsNullOrEmpty(State.Cid)) throw new Exception("No cid!");

            var response = await codex.RequestStorage(new CodexPlugin.ContentId(State.Cid));
            if (string.IsNullOrEmpty(response) ||
                response == "Unable to encode manifest" ||
                response == "Purchasing not available" ||
                response == "Expiry required" ||
                response == "Expiry needs to be in future" ||
                response == "Expiry has to be before the request's end (now + duration)")
            {
                throw new InvalidOperationException(response);
            }

            State.Purchases = State.Purchases.Concat([
                new WorkerPurchase
                {
                    Created = DateTime.UtcNow,
                    Pid = response
                }
            ]).ToArray();

            app.Log.Log($"New purchase created for '{sourceFilename}'. PID: '{response}'");
            Thread.Sleep(500);
        }

        private async Task EnsureCid(ICodexInstance instance, CodexNode codex)
        {
            app.Log.Log($"Ensuring CID for '{sourceFilename}'...");
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
                    app.Log.Log($"Existing CID '{State.Cid}' for '{sourceFilename}' could not be found in the network.");
                    State.Cid = "";
                }
                else
                {
                    app.Log.Log($"Existing CID '{State.Cid}' for '{sourceFilename}' was successfully found in the network.");
                }
            }

            if (string.IsNullOrEmpty(State.Cid))
            {
                app.Log.Log($"Uploading '{sourceFilename}'...");
                var cid = await codex.UploadFile(sourceFilename);
                app.Log.Log("Got CID: " + cid);
                State.Cid = cid.Id;
                Thread.Sleep(1000);
            }
        }

        private WorkerPurchase? GetMostRecent()
        {
            if (!State.Purchases.Any()) return null;
            var maxCreated = State.Purchases.Max(p => p.Created);
            return State.Purchases.SingleOrDefault(p => p.Created == maxCreated);
        }

        public bool IsCurrentlyRunning()
        {
            if (!State.Purchases.Any()) return false;

            return State.Purchases.Any(p =>
                p.Submitted.HasValue &&
                p.Started.HasValue &&
                !p.Expiry.HasValue &&
                !p.Finish.HasValue &&
                p.Started.Value > DateTime.UtcNow - purchaseInfo.PurchaseDurationTotal
            );
        }

        public bool IsCurrentlyFailed()
        {
            if (!State.Purchases.Any()) return false;

            var mostRecent = GetMostRecent();
            if (mostRecent == null) return false;

            return mostRecent.Expiry.HasValue;
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
