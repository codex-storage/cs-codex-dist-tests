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
}
