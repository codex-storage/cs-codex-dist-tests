﻿using AutoClient.Modes.FolderStore;

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
                if (worker.FailureCounter > 5)
                {
                    throw new Exception("Worker has failure count > 5. Stopping AutoClient...");
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
            var worker = new FileWorker(app, instance, purchaseInfo, folder, file, OnFileUploaded, OnNewPurchase);
            worker.Update();
            if (worker.IsBusy()) app.FolderWorkDispatcher.WorkerIsBusy();
            return worker;
        }

        private void OnFileUploaded()
        {
        }

        private void OnNewPurchase()
        {
            app.FolderWorkDispatcher.ResetIndex();

            var overview = new FolderWorkOverview(app, purchaseInfo, folder);
            overview.MarkUncommitedChange();
        }

        public void Stop()
        {
            cts.Cancel();
            checkTask.Wait();
        }
    }
}
