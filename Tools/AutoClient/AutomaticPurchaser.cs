﻿using Logging;

namespace AutoClient
{
    public class AutomaticPurchaser
    {
        private readonly App app;
        private readonly ILog log;
        private readonly CodexWrapper node;
        private Task workerTask = Task.CompletedTask;

        public AutomaticPurchaser(App app, ILog log, CodexWrapper node)
        {
            this.app = app;
            this.log = log;
            this.node = node;
        }

        public void Start()
        {
            workerTask = Task.Run(Worker);
        }

        public void Stop()
        {
            workerTask.Wait();
        }

        private async Task Worker()
        {
            log.Log("Worker started.");
            while (!app.Cts.Token.IsCancellationRequested)
            {
                try
                {
                    var pid = await StartNewPurchase();
                    await WaitTillFinished(pid);
                }
                catch (Exception ex)
                {
                    log.Error("Worker failed with: " + ex);
                    await Task.Delay(TimeSpan.FromHours(6));
                }
            }
        }

        private async Task<string> StartNewPurchase()
        {
            var file = await CreateFile();
            try
            {
                var cid = node.UploadFile(file);
                var response = node.RequestStorage(cid);
                return response.PurchaseId;
            }
            finally
            {
                DeleteFile(file);
            }
        }

        private async Task<string> CreateFile()
        {
            return await app.Generator.Generate();
        }

        private void DeleteFile(string file)
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception exc)
            {
                log.Error($"Failed to delete file '{file}': {exc}");
            }
        }

        private async Task WaitTillFinished(string pid)
        {
            try
            {
                var emptyResponseTolerance = 10;
                while (!app.Cts.Token.IsCancellationRequested)
                {
                    var purchase = node.GetStoragePurchase(pid);
                    if (purchase == null)
                    {
                        await FixedShortDelay();
                        emptyResponseTolerance--;
                        if (emptyResponseTolerance == 0)
                        {
                            log.Log("Received 10 empty responses. Stop tracking this purchase.");
                            await ExpiryTimeDelay();
                            return;
                        }
                        continue;
                    }
                    if (purchase.IsCancelled)
                    {
                        app.Performance.StorageContractCancelled();
                        return;
                    }
                    if (purchase.IsError)
                    {
                        app.Performance.StorageContractErrored(purchase.Error);
                        return;
                    }
                    if (purchase.IsFinished)
                    {
                        app.Performance.StorageContractFinished();
                        return;
                    }
                    if (purchase.IsStarted)
                    {
                        app.Performance.StorageContractStarted();
                        await FixedDurationDelay();
                    }

                    await FixedShortDelay();
                }
            }
            catch (Exception ex)
            {
                log.Log($"Wait failed with exception: {ex}. Assume contract will expire: Wait expiry time.");
                await ExpiryTimeDelay();
            }
        }

        private async Task FixedDurationDelay()
        {
            await Task.Delay(app.Config.ContractDurationMinutes * 60 * 1000, app.Cts.Token);
        }

        private async Task ExpiryTimeDelay()
        {
            await Task.Delay(app.Config.ContractExpiryMinutes * 60 * 1000, app.Cts.Token);
        }

        private async Task FixedShortDelay()
        {
            await Task.Delay(15 * 1000, app.Cts.Token);
        }
    }
}
