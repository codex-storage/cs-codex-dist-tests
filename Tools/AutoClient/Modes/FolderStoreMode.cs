using AutoClient.Modes.FolderStore;

namespace AutoClient.Modes
{
    public class FolderStoreMode : IMode, IWorkEventHandler
    {
        private readonly App app;
        private readonly string folder;
        private readonly PurchaseInfo purchaseInfo;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private Task checkTask = Task.CompletedTask;
        private int failureCount = 0;

        public FolderStoreMode(App app, string folder, PurchaseInfo purchaseInfo)
        {
            this.app = app;
            this.folder = folder;
            this.purchaseInfo = purchaseInfo;
        }

        public void Start(CodexWrapper instance, int index)
        {
            checkTask = Task.Run(() =>
            {
                try
                {
                    RunChecker(instance);
                }
                catch (Exception ex)
                {
                    app.Log.Error("Exception in FolderStoreMode worker: " + ex);
                    Environment.Exit(1);
                }
            });
        }

        private void RunChecker(CodexWrapper instance)
        {
            var i = 0;
            while (!cts.IsCancellationRequested)
            {
                Thread.Sleep(2000);

                var worker = ProcessWorkItem(instance);
                if (failureCount > 5)
                {
                    throw new Exception("Failure count > 5. Stopping AutoClient...");
                }
                i++;

                if (i > 5)
                {
                    i = 0;
                    var overview = new FolderWorkOverview(app, purchaseInfo, folder);
                    overview.Update(instance);
                }
            }
        }

        private FileWorker ProcessWorkItem(CodexWrapper instance)
        {
            var file = app.FolderWorkDispatcher.GetFileToCheck();
            var worker = new FileWorker(app, instance, purchaseInfo, folder, file, this);
            worker.Update();
            if (worker.IsBusy()) app.FolderWorkDispatcher.WorkerIsBusy();
            return worker;
        }

        public void OnFileUploaded()
        {
        }

        public void OnNewPurchase()
        {
            app.FolderWorkDispatcher.ResetIndex();

            var overview = new FolderWorkOverview(app, purchaseInfo, folder);
            overview.MarkUncommitedChange();
        }

        public void OnPurchaseExpired()
        {
            failureCount++;
        }

        public void OnPurchaseStarted()
        {
            failureCount = 0;
        }

        public void Stop()
        {
            cts.Cancel();
            checkTask.Wait();
        }
    }
}
