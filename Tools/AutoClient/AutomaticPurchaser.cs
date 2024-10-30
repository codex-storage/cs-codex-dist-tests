using CodexOpenApi;
using CodexPlugin;
using Logging;
using Newtonsoft.Json;
using Utils;

namespace AutoClient
{
    public class AutomaticPurchaser
    {
        private readonly ILog log;
        private readonly ICodexInstance codex;
        private Task workerTask = Task.CompletedTask;
        private App app => codex.App;

        public AutomaticPurchaser(ILog log, ICodexInstance codex)
        {
            this.log = log;
            this.codex = codex;
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
                    await DownloadForeignCid();
                }
                catch (Exception ex)
                {
                    log.Error("Worker failed with: " + ex);
                    await Task.Delay(TimeSpan.FromHours(6));
                }
            }
        }

        private async Task DownloadForeignCid()
        {
            var cid = app.CidRepo.GetForeignCid(codex.NodeId);
            if (cid == null) return;
            var size = app.CidRepo.GetSizeForCid(cid);
            if (size == null) return;

            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var filename = Guid.NewGuid().ToString().ToLowerInvariant();
                {
                    using var fileStream = File.OpenWrite(filename);
                    var fileResponse = await codex.Codex.DownloadNetworkStreamAsync(cid);
                    fileResponse.Stream.CopyTo(fileStream);
                }
                var time = sw.Elapsed;
                File.Delete(filename);
                app.Performance.DownloadSuccessful(size.Value, time);
            }
            catch (Exception ex)
            {
                app.Performance.DownloadFailed(ex);
            }
        }

        private async Task<string> StartNewPurchase()
        {
            var file = await CreateFile();
            try
            {
                var cid = await UploadFile(file);
                return await RequestStorage(cid);
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
                app.Log.Error($"Failed to delete file '{file}': {exc}");
            }
        }

        private async Task<ContentId> UploadFile(string filename)
        {
            using var fileStream = File.OpenRead(filename);
            try
            {
                var info = new FileInfo(filename);
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var cid = await UploadStream(fileStream, filename);
                var time = sw.Elapsed;
                app.Performance.UploadSuccessful(info.Length, time);
                app.CidRepo.Add(codex.NodeId, cid.Id, info.Length);
                return cid;
            }
            catch (Exception exc)
            {
                app.Performance.UploadFailed(exc);
                throw;
            }
        }

        private async Task<ContentId> UploadStream(FileStream fileStream, string filename)
        {
            log.Debug($"Uploading file...");
            var response = await codex.Codex.UploadAsync(
                content_type: "application/x-binary",
                content_disposition: $"attachment; filename=\"{filename}\"",
                fileStream, app.Cts.Token);

            if (string.IsNullOrEmpty(response)) FrameworkAssert.Fail("Received empty response.");
            if (response.StartsWith("Unable to store block")) FrameworkAssert.Fail("Node failed to store block.");

            log.Debug($"Uploaded file. Received contentId: '{response}'.");
            return new ContentId(response);
        }

        private async Task<string> RequestStorage(ContentId cid)
        {
            log.Debug("Requesting storage for " + cid.Id);
            var result = await codex.Codex.CreateStorageRequestAsync(cid.Id, new StorageRequestCreation()
            {
                Collateral = app.Config.RequiredCollateral.ToString(),
                Duration = (app.Config.ContractDurationMinutes * 60).ToString(),
                Expiry = (app.Config.ContractExpiryMinutes * 60).ToString(),
                Nodes = app.Config.NumHosts,
                Reward = app.Config.Price.ToString(),
                ProofProbability = "15",
                Tolerance = app.Config.HostTolerance
            }, app.Cts.Token);

            log.Debug("Purchase ID: " + result);

            var encoded = await GetEncodedCid(result);
            app.CidRepo.AddEncoded(cid.Id, encoded);

            return result;
        }

        private async Task<string> GetEncodedCid(string pid)
        {
            try
            {
                var sp = (await GetStoragePurchase(pid))!;
                return sp.Request.Content.Cid;
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
                throw;
            }
        }

        private async Task<StoragePurchase?> GetStoragePurchase(string pid)
        {
            // openapi still don't match code.
            var str = await codex.Client.GetStringAsync($"{codex.Address.Host}:{codex.Address.Port}/api/codex/v1/storage/purchases/{pid}");
            if (string.IsNullOrEmpty(str)) return null;
            return JsonConvert.DeserializeObject<StoragePurchase>(str);
        }

        private async Task WaitTillFinished(string pid)
        {
            try
            {
                var emptyResponseTolerance = 10;
                while (!app.Cts.Token.IsCancellationRequested)
                {
                    var purchase = await GetStoragePurchase(pid);
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
                    var status = purchase.State.ToLowerInvariant();
                    if (status.Contains("cancel"))
                    {
                        app.Performance.StorageContractCancelled();
                        return;
                    }
                    if (status.Contains("error"))
                    {
                        app.Performance.StorageContractErrored(purchase.Error);
                        return;
                    }
                    if (status.Contains("finished"))
                    {
                        app.Performance.StorageContractFinished();
                        return;
                    }
                    if (status.Contains("started"))
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
