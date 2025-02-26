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

        public void Start(CodexWrapper instance, int index)
        {
            checkTask = Task.Run(() =>
            {
                try
                {
                    var saver = new FolderSaver(app, instance);
                    while (!cts.IsCancellationRequested)
                    {
                        saver.Run(cts);
                    }
                }
                catch (Exception ex)
                {
                    app.Log.Error("Exception in FolderStoreMode: " + ex);
                    Environment.Exit(1);
                }
            });
        }

        public void Stop()
        {
            cts.Cancel();
            checkTask.Wait();
        }
    }
}
