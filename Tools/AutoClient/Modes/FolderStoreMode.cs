using AutoClient.Modes.FolderStore;

namespace AutoClient.Modes
{
    public class FolderStoreMode
    {
        private readonly App app;
        private Task checkTask = Task.CompletedTask;
        private readonly LoadBalancer loadBalancer;

        public FolderStoreMode(App app, LoadBalancer loadBalancer)
        {
            this.app = app;
            this.loadBalancer = loadBalancer;
        }

        public void Start()
        {
            checkTask = Task.Run(() =>
            {
                try
                {
                    var saver = new FolderSaver(app, loadBalancer);
                    while (!app.Cts.IsCancellationRequested)
                    {
                        saver.Run();
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
            app.Cts.Cancel();
            checkTask.Wait();
        }
    }
}
