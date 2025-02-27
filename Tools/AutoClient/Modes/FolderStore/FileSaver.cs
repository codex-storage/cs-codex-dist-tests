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

        public FileSaver(ILog log, CodexWrapper instance, Stats stats, string folderFile, FileStatus entry)
        {
            this.log = log;
            this.instance = instance;
            this.stats = stats;
            this.folderFile = folderFile;
            this.entry = entry;
        }

        public bool HasFailed { get; private set; }
        public bool Changes { get; private set; }

        public void Process()
        {
            HasFailed = false;
            Changes = false;
            if (HasRecentPurchase(entry))
            {
                Log($"Purchase running: '{entry.PurchaseId}'");
                return;
            }

            EnsureBasicCid();
            CreateNewPurchase();
        }

        private void EnsureBasicCid()
        {
            if (IsBasicCidAvailable()) return;
            UploadFile();
        }

        private bool IsBasicCidAvailable()
        {
            if (string.IsNullOrEmpty(entry.BasicCid)) return false;
            return NodeContainsBasicCid();
        }

        private bool HasRecentPurchase(FileStatus entry)
        {
            if (string.IsNullOrEmpty(entry.PurchaseId)) return false;
            var purchase = GetPurchase(entry.PurchaseId);
            if (purchase == null) return false;
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
                var result = instance.Node.DownloadManifestOnly(new ContentId(entry.BasicCid));
                return !string.IsNullOrEmpty(result.Cid.Id);
            }
            catch
            {
                Log("Failed to download manifest for basicCid");
                return false;
            }
        }

        private void UploadFile()
        {
            Changes = true;
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
        }

        private void CreateNewPurchase()
        {
            if (string.IsNullOrEmpty(entry.BasicCid)) return;

            Changes = true;
            try
            {
                var request = CreateNewStorageRequest();

                WaitForSubmitted(request);
                WaitForStarted(request);

                entry.PurchaseFinishedUtc = DateTime.UtcNow + request.Purchase.Duration;
                stats.StorageRequestStats.SuccessfullyStarted++;
                Log($"Successfully started new purchase: '{entry.PurchaseId}' for {Time.FormatDuration(request.Purchase.Duration)}");
            }
            catch (Exception exc)
            {
                entry.EncodedCid = string.Empty;
                entry.PurchaseId = string.Empty;
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
                return request;
            }
            catch
            {
                stats.StorageRequestStats.FailedToCreate++;
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

        private void Log(string msg)
        {
            log.Log(msg);
        }
    }
}
