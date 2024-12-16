using CodexOpenApi;
using System.IO.Compression;
using static AutoClient.Modes.FolderStore.FolderWorkOverview;

namespace AutoClient.Modes.FolderStore
{
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

        protected override void OnNewState(WorkMonitorStatus newState)
        {
            newState.LastOverviewUpdate = DateTime.MinValue;
        }

        public async Task Update(ICodexInstance instance)
        {
            var jsonFiles = Directory.GetFiles(Folder).Where(f => f.ToLowerInvariant().EndsWith(".json") && !f.Contains(OverviewFilename)).ToList();

            var total = 0;
            var successful = 0;
            var failed = 0;
            foreach (var file in jsonFiles)
            {
                try
                {
                    var worker = new FileStatus(app, Folder, file.Substring(0, file.Length - 5), purchaseInfo);
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

            if (State.UncommitedChanges > 3)
            {
                State.UncommitedChanges = 0;
                SaveState();

                await CreateNewOverviewZip(jsonFiles, FilePath, instance);
            }
        }

        public void MarkUncommitedChange()
        {
            State.UncommitedChanges++;
            SaveState();
        }

        private async Task CreateNewOverviewZip(List<string> jsonFiles, string filePath, ICodexInstance instance)
        {
            Log("");
            Log("");
            Log("Creating new overview zipfile...");
            var zipFilename = CreateZipFile(jsonFiles, filePath);

            Log("Uploading to Codex...");
            try
            {
                var codex = new CodexNode(app, instance);
                var cid = await codex.UploadFile(zipFilename);
                Log($"Upload successful: New overview zipfile CID = '{cid.Id}'");
                Log("Requesting storage for it...");
                var result = await codex.RequestStorage(cid);
                Log("Storage requested. Purchase ID: " + result);

                var outFile = Path.Combine(app.Config.DataPath, "OverviewZip.cid");
                File.AppendAllLines(outFile, [DateTime.UtcNow.ToString("o") + " - " + result.EncodedCid.Id]);
                Log($">>> [{outFile}] has been updated. <<<");
            }
            catch (Exception exc)
            {
                Log("Failed to upload new overview zipfile: " + exc);
            }
            Log("");
            Log("");
        }

        private string CreateZipFile(List<string> jsonFiles, string filePath)
        {
            var zipFilename = Guid.NewGuid().ToString() + ".zip";

            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    archive.CreateEntryFromFile(filePath, "overview.json");
                    foreach (var file in jsonFiles)
                    {
                        archive.CreateEntryFromFile(file, Path.GetFileName(file));
                    }
                }

                using (var fileStream = new FileStream(zipFilename, FileMode.Create))
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    memoryStream.CopyTo(fileStream);
                }
            }
            return zipFilename;
        }

        private void Log(string msg)
        {
            app.Log.Log(msg);
        }

        [Serializable]
        public class WorkMonitorStatus
        {
            public int TotalFiles { get; set; }
            public int SuccessfulStored { get; set; }
            public int StoreFailed { get; set; }

            public DateTime LastOverviewUpdate { get; set; }
            public int UncommitedChanges { get; set; }
        }
    }
}
