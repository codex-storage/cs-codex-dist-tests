using Newtonsoft.Json;
using static AutoClient.Modes.FileWorker;
using static AutoClient.Modes.FolderWorkOverview;

namespace AutoClient.Modes
{
    public class FolderStoreMode : IMode
    {
        private readonly App app;
        private readonly string folder;
        private readonly PurchaseInfo purchaseInfo;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private Task checkTask = Task.CompletedTask;

        public FolderStoreMode(App app, string folder, PurchaseInfo purchaseInfo)
        {
            this.app = app;
            this.folder = folder;
            this.purchaseInfo = purchaseInfo;
        }

        public void Start(ICodexInstance instance, int index)
        {
            checkTask = Task.Run(async () =>
            {
                try
                {
                    await RunChecker(instance);
                }
                catch (Exception ex)
                {
                    app.Log.Error("Exception in FolderStoreMode worker: " + ex);
                    Environment.Exit(1);
                }
            });
        }

        private async Task RunChecker(ICodexInstance instance)
        {
            var i = 0;
            while (!cts.IsCancellationRequested)
            {
                if (app.FolderWorkDispatcher.Revisiting) Thread.Sleep(2000);
                else Thread.Sleep(5000);

                var worker = await ProcessWorkItem(instance);
                if (worker.FailureCounter > 5)
                {
                    throw new Exception("Worker  has failure count > 5. Stopping AutoClient...");
                }
                i++;

                if (i > 5)
                {
                    i = 0;
                    var overview = new FolderWorkOverview(app, purchaseInfo, folder);
                    overview.Update();
                }
            }
        }

        private async Task<FileWorker> ProcessWorkItem(ICodexInstance instance)
        {
            var file = app.FolderWorkDispatcher.GetFileToCheck();
            var worker = new FileWorker(app, purchaseInfo, folder, file);
            await worker.Update(instance, () =>
            {
                app.FolderWorkDispatcher.RevisitSoon(file);
            });

            return worker;
        }

        public void Stop()
        {
            cts.Cancel();
            checkTask.Wait();
        }
    }

    public class PurchaseInfo
    {
        public TimeSpan PurchaseDurationTotal { get; set; }
        public TimeSpan PurchaseDurationSafe { get; set; }
    }

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
                (recent.Created + purchaseInfo.PurchaseDurationSafe) > DateTime.UtcNow)
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
                p.Started.Value > (DateTime.UtcNow - purchaseInfo.PurchaseDurationTotal)
            );
        }

        public bool IsCurrentlyFailed()
        {
            if (!State.Purchases.Any()) return false;

            var mostRecent = GetMostRecent();
            if (mostRecent == null ) return false;

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
            public DateTime? Finish {  get; set; }
        }
    }

    public class FolderWorkDispatcher
    {
        private readonly List<string> files = new List<string>();
        private readonly List<string> revisitSoon = new List<string>();
        public bool Revisiting { get; private set; } = false;

        public FolderWorkDispatcher(string folder)
        {
            var fs = Directory.GetFiles(folder);
            foreach (var f in fs)
            {
                if (!f.ToLowerInvariant().Contains(".json"))
                {
                    var info = new FileInfo(f);
                    if (info.Exists && info.Length > (1024 * 1024)) // larger than 1MB
                    {
                        files.Add(f);
                    }
                }
            }
        }

        public string GetFileToCheck()
        {
            if (Revisiting)
            {
                if (!revisitSoon.Any())
                {
                    Revisiting = false;
                    return GetFileToCheck();
                }

                var file = revisitSoon.First();
                revisitSoon.RemoveAt(0);
                return file;
            }
            else
            {
                var file = files.First();
                files.RemoveAt(0);
                files.Add(file);

                if (revisitSoon.Count > 3) Revisiting = true;
                return file;
            }
        }

        public void RevisitSoon(string file)
        {
            revisitSoon.Add(file);
        }
    }

    public class FolderWorkOverview : JsonBacked<WorkMonitorStatus>
    {
        private const string OverviewFilename = "codex_folder_saver_overview.json";
        private readonly App app;
        private readonly PurchaseInfo purchaseInfo;

        public FolderWorkOverview(App app, PurchaseInfo purchaseInfo, string folder)
            : base(app, folder, Path.Combine(folder, OverviewFilename))
        {
            this.app = app;
            this.purchaseInfo = purchaseInfo;
        }

        public void Update()
        {
            var jsonFiles = Directory.GetFiles(Folder).Where(f => f.ToLowerInvariant().EndsWith(".json") && !f.Contains(OverviewFilename)).ToList();

            var total = 0;
            var successful = 0;
            var failed = 0;
            foreach (var file in jsonFiles)
            {
                try
                {
                    var worker = new FileWorker(app, purchaseInfo, Folder, file.Substring(0, file.Length - 5));
                    total++;
                    if (worker.IsCurrentlyRunning()) successful++;
                    if (worker.IsCurrentlyFailed()) failed++;
                }
                catch (Exception exc)
                {
                    app.Log.Error("Exception in workoverview update: " + exc);
                }
            }

            State.TotalFiles = total;
            State.SuccessfulStored = successful;
            State.StoreFailed = failed;
            SaveState();
        }

        [Serializable]
        public class WorkMonitorStatus
        {
            public int TotalFiles { get; set; }
            public int SuccessfulStored { get; set; }
            public int StoreFailed { get; set; }
        }
    }

    public abstract class JsonBacked<T> where T : new()
    {
        private readonly App app;

        protected JsonBacked(App app, string folder, string filePath)
        {
            this.app = app;
            Folder = folder;
            FilePath = filePath;
            LoadState();
        }

        private void LoadState()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    State = new T();
                    SaveState();
                }
                var text = File.ReadAllText(FilePath);
                State = JsonConvert.DeserializeObject<T>(text)!;
                if (State == null) throw new Exception("Didn't deserialize " + FilePath);
            }
            catch (Exception exc)
            {
                app.Log.Error("Failed to load state: " + exc);
            }
        }

        protected string Folder { get; }
        protected string FilePath { get; }
        protected T State { get; private set; } = default(T)!;

        protected void SaveState()
        {
            try
            {
                var json = JsonConvert.SerializeObject(State);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception exc)
            {
                app.Log.Error("Failed to save state: " + exc);
            }
        }
    }
}
