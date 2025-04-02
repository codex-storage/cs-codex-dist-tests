using CodexClient;
using Logging;
using Utils;

namespace AutoClient.Modes.FolderStore
{
    public class FileSaver
    {
        private readonly ILog log;
        private readonly CodexWrapper instance;
        private readonly Stats stats;
        private readonly string folderFile;
        private readonly FileStatus entry;
        private readonly Action saveChanges;
        private readonly QuotaCheck quotaCheck;

        public FileSaver(ILog log, CodexWrapper instance, Stats stats, string folderFile, FileStatus entry, Action saveChanges)
        {
            this.log = log;
            this.instance = instance;
            this.stats = stats;
            this.folderFile = folderFile;
            this.entry = entry;
            this.saveChanges = saveChanges;
            quotaCheck = new QuotaCheck(log, folderFile, instance);
        }

        public bool HasFailed { get; private set; }

        public void Process()
        {
            HasFailed = false;
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
            HasFailed = true;
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
            return instance.GetStoragePurchase(purchaseId);
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
                HasFailed = true;
            }
            saveChanges();
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
                saveChanges();

                Log($"Successfully started new purchase: '{entry.PurchaseId}' for {Time.FormatDuration(request.Purchase.Duration)}");
            }
            catch (Exception exc)
            {
                entry.EncodedCid = string.Empty;
                entry.PurchaseId = string.Empty;
                saveChanges();
                log.Error("Failed to start new purchase: " + exc);
                HasFailed = true;
            }
        }

        private IStoragePurchaseContract CreateNewStorageRequest()
        {
            try
            {
                var request = instance.RequestStorage(new ContentId(entry.BasicCid));
                entry.EncodedCid = request.Purchase.ContentId.Id;
                entry.PurchaseId = request.PurchaseId;
                entry.PurchaseFinishedUtc = DateTime.UtcNow + request.Purchase.Duration;

                if (!IsCidEncoded(entry.EncodedCid))
                {
                    log.Error("CID received from storage request is not protected/encoded.");
                    throw new Exception("CID received from storage request was not protected.");
                }

                saveChanges();
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
                var expirySeconds = Convert.ToInt64(purchase.Request.Expiry);
                var expiry = TimeSpan.FromSeconds(expirySeconds);
                Log($"Request was submitted but not started yet. Waiting {Time.FormatDuration(expiry)} to start or expire...");

                var limit = DateTime.UtcNow + expiry;
                while (DateTime.UtcNow < limit)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(30));
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
                            saveChanges();
                            return;
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                HasFailed = true;
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
