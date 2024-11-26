using AutoClient.Modes.FolderStore;

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
                    throw new Exception("Worker has failure count > 5. Stopping AutoClient...");
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
}
