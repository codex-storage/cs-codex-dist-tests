using CodexClient;
using Logging;
using Utils;

namespace AutoClient.Modes.FolderStore
{
    public interface IFileSaverEventHandler
    {
        void SaveChanges();
    }

    public interface IFileSaverResultHandler
    {
        void OnSuccess();
        void OnFailure();
    }

    public class FileSaver
    {
        private readonly ILog log;
        private readonly LoadBalancer loadBalancer;
        private readonly Stats stats;
        private readonly string folderFile;
        private readonly FileStatus entry;
        private readonly IFileSaverEventHandler saveHandler;
        private readonly IFileSaverResultHandler resultHandler;

        public FileSaver(ILog log, LoadBalancer loadBalancer, Stats stats, string folderFile, FileStatus entry, IFileSaverEventHandler saveHandler, IFileSaverResultHandler resultHandler)
        {
            this.log = log;
            this.loadBalancer = loadBalancer;
            this.stats = stats;
            this.folderFile = folderFile;
            this.entry = entry;
            this.saveHandler = saveHandler;
            this.resultHandler = resultHandler;
        }

        public void Process()
        {
            if (string.IsNullOrEmpty(entry.CodexNodeId))
            {
                DispatchToAny();
            }
            else
            {
                DispatchToSpecific();
            }
        }

        private void DispatchToAny()
        {
            loadBalancer.DispatchOnCodex(instance =>
            {
                entry.CodexNodeId = instance.Node.GetName();
                saveHandler.SaveChanges();

                var run = new FileSaverRun(log, instance, stats, folderFile, entry, saveHandler, resultHandler);
                run.Process();
            });
        }

        private void DispatchToSpecific()
        {
            loadBalancer.DispatchOnSpecificCodex(instance =>
            {
                var run = new FileSaverRun(log, instance, stats, folderFile, entry, saveHandler, resultHandler);
                run.Process();
            }, entry.CodexNodeId);
        }
    }

    public class FileSaverRun
    {
        private readonly ILog log;
        private readonly CodexWrapper instance;
        private readonly Stats stats;
        private readonly string folderFile;
        private readonly FileStatus entry;
        private readonly IFileSaverEventHandler saveHandler;
        private readonly IFileSaverResultHandler resultHandler;
        private readonly QuotaCheck quotaCheck;

        public FileSaverRun(ILog log, CodexWrapper instance, Stats stats, string folderFile, FileStatus entry, IFileSaverEventHandler saveHandler, IFileSaverResultHandler resultHandler)
        {
            this.log = log;
            this.instance = instance;
            this.stats = stats;
            this.folderFile = folderFile;
            this.entry = entry;
            this.saveHandler = saveHandler;
            this.resultHandler = resultHandler;
            quotaCheck = new QuotaCheck(log, folderFile, instance);
        }

        public void Process()
        {
            if (HasRecentPurchase())
            {
                Log($"Purchase running: '{entry.PurchaseId}'");
                return;
            }

            EnsureBasicCid();
            CreateNewPurchase();
        }

        private void EnsureBasicCid()
        {
            if (IsBasicCidAvailable())
            {
                Log("BasicCid is available.");
                return;
            }
            if (QuotaAvailable())
            {
                UploadFile();
            }
        }

        private bool IsBasicCidAvailable()
        {
            if (string.IsNullOrEmpty(entry.BasicCid)) return false;
            return NodeContainsBasicCid();
        }

        private bool QuotaAvailable()
        {
            if (quotaCheck.IsLocalQuotaAvailable()) return true;
            Log("Waiting for local storage quota to become available...");

            var timeLimit = DateTime.UtcNow + TimeSpan.FromHours(1.0);
            while (DateTime.UtcNow < timeLimit)
            {
                if (quotaCheck.IsLocalQuotaAvailable()) return true;
                Thread.Sleep(TimeSpan.FromMinutes(1.0));
            }
            Log("Could not upload: Insufficient local storage quota.");
            resultHandler.OnFailure();
            return false;
        }

        private bool HasRecentPurchase()
        {
            if (string.IsNullOrEmpty(entry.PurchaseId)) return false;
            var purchase = GetPurchase(entry.PurchaseId);
            if (purchase == null) return false;
            if (purchase.IsSubmitted)
            {
                WaitForSubmittedToStarted(purchase);
            }
            if (!purchase.IsStarted) return false;

            // Purchase is started. But, if it finishes soon, we will treat it as already finished.
            var threshold = DateTime.UtcNow + TimeSpan.FromHours(3.0);
            if (entry.PurchaseFinishedUtc < threshold)
            {
                Log($"Running purchase will expire soon.");
                return false;
            }
            return true;
        }

        private StoragePurchase? GetPurchase(string purchaseId)
        {
            try
            {
                return instance.GetStoragePurchase(purchaseId);
            }
            catch (Exception exc)
            {
                log.Error("Failed to get purchase: " + exc);
                return null;
            }
        }

        private bool NodeContainsBasicCid()
        {
            try
            {
                var result = instance.Node.LocalFiles();
                if (result == null) return false;
                if (result.Content == null) return false;

                var localCids = result.Content.Where(c => !string.IsNullOrEmpty(c.Cid.Id)).Select(c => c.Cid.Id).ToArray();
                var isFound = localCids.Any(c => c.ToLowerInvariant() == entry.BasicCid.ToLowerInvariant());
                if (isFound)
                {
                    Log("BasicCid found in local files.");

                    if (IsCidBasic(entry.BasicCid))
                    {
                        Log("BasicCid is confirmed to not be encoded.");
                        return true;
                    }

                    entry.BasicCid = string.Empty;
                    Log("Warning: Cid stored as basic was actually encoded!");
                    return false;
                }
            }
            catch (Exception exc)
            {
                Log($"Exception in {nameof(NodeContainsBasicCid)}: {exc}");
                throw;
            }

            Log("BasicCid not found in local files.");
            return false;
        }

        private void UploadFile()
        {
            Log("Uploading file...");
            try
            {
                entry.BasicCid = instance.UploadFile(folderFile).Id;
                stats.SuccessfulUploads++;
                Log($"Successfully uploaded. BasicCid: '{entry.BasicCid}'");
            }
            catch (Exception exc)
            {
                entry.BasicCid = string.Empty;
                stats.FailedUploads++;
                log.Error("Failed to upload: " + exc);
                resultHandler.OnFailure();
            }
            saveHandler.SaveChanges();
        }

        private void CreateNewPurchase()
        {
            if (string.IsNullOrEmpty(entry.BasicCid)) return;
            Log("Creating new purchase...");

            try
            {
                var request = CreateNewStorageRequest();

                WaitForSubmitted(request);
                WaitForStarted(request);

                stats.StorageRequestStats.SuccessfullyStarted++;
                saveHandler.SaveChanges();

                Log($"Successfully started new purchase: '{entry.PurchaseId}' for {Time.FormatDuration(request.Purchase.Duration)}");
                resultHandler.OnSuccess();
            }
            catch (Exception exc)
            {
                entry.EncodedCid = string.Empty;
                entry.PurchaseId = string.Empty;
                stats.StorageRequestStats.FailedToStart++;
                saveHandler.SaveChanges();
                log.Error("Failed to start new purchase: " + exc);
                resultHandler.OnFailure();
            }
        }

        private IStoragePurchaseContract CreateNewStorageRequest()
        {
            try
            {
                var request = instance.RequestStorage(new ContentId(entry.BasicCid));
                entry.EncodedCid = request.ContentId.Id;
                entry.PurchaseId = request.PurchaseId;
                entry.PurchaseFinishedUtc = DateTime.UtcNow + request.Purchase.Duration;

                if (!IsCidEncoded(entry.EncodedCid))
                {
                    log.Error("CID received from storage request is not protected/encoded.");
                    throw new Exception("CID received from storage request was not protected.");
                }

                Log("Saved new purchaseId: " + entry.PurchaseId);
                return request;
            }
            catch
            {
                stats.StorageRequestStats.FailedToCreate++;
                throw;
            }
        }

        private void WaitForSubmittedToStarted(StoragePurchase purchase)
        {
            try
            {
                if (purchase.IsStarted) return;

                var expirySeconds = Convert.ToInt64(purchase.Request.Expiry);
                var expiry = TimeSpan.FromSeconds(expirySeconds);
                Log($"Request was submitted but not started yet. Waiting {Time.FormatDuration(expiry)} to start or expire...");

                var limit = DateTime.UtcNow + expiry;
                while (DateTime.UtcNow < limit)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                    var update = GetPurchase(purchase.Request.Id);
                    if (update != null)
                    {
                        if (update.IsStarted)
                        {
                            Log("Request successfully started.");
                            return;
                        }
                        else if (!update.IsSubmitted)
                        {
                            Log("Request failed to start. State: " + update.State);
                            entry.EncodedCid = string.Empty;
                            entry.PurchaseId = string.Empty;
                            stats.StorageRequestStats.FailedToStart++;
                            saveHandler.SaveChanges();
                            return;
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                resultHandler.OnFailure();
                Log($"Exception in {nameof(WaitForSubmittedToStarted)}: {exc}");
                throw;
            }
        }

        private void WaitForSubmitted(IStoragePurchaseContract request)
        {
            try
            {
                request.WaitForStorageContractSubmitted();
            }
            catch
            {
                stats.StorageRequestStats.FailedToSubmit++;
                throw;
            }
        }

        private void WaitForStarted(IStoragePurchaseContract request)
        {
            try
            {
                request.WaitForStorageContractStarted();
            }
            catch
            {
                stats.StorageRequestStats.FailedToStart++;
                throw;
            }
        }

        private bool IsCidEncoded(string cid)
        {
            try
            {
                return GetManifestIsProtected(cid);
            }
            catch (Exception ex)
            {
                log.Error(nameof(IsCidEncoded) + ": " + ex);
                return false;
            }
        }

        private bool IsCidBasic(string cid)
        {
            try
            {
                return !GetManifestIsProtected(cid);
            }
            catch (Exception ex)
            {
                log.Error(nameof(IsCidBasic) + ": " + ex);
                return false;
            }
        }

        private bool GetManifestIsProtected(string cid)
        {
            var id = new ContentId(cid);
            var manifest = instance.Node.DownloadManifestOnly(id);
            return manifest.Manifest.Protected;
        }

        private void Log(string msg)
        {
            log.Log(msg);
        }
    }
}
