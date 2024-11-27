using static AutoClient.Modes.FolderStore.FileWorker;

namespace AutoClient.Modes.FolderStore
{
    public class FileStatus : JsonBacked<WorkerStatus>
    {
        private readonly PurchaseInfo purchaseInfo;

        public FileStatus(App app, string folder, string filePath, PurchaseInfo purchaseInfo)
            : base(app, folder, filePath + ".json")
        {
            this.purchaseInfo = purchaseInfo;
        }

        public bool IsBusy()
        {
            if (!State.Purchases.Any()) return false;

            return State.Purchases.Any(p =>
                p.Submitted.HasValue &&
                !p.Started.HasValue &&
                !p.Expiry.HasValue &&
                !p.Finish.HasValue &&
                p.Created > DateTime.UtcNow - purchaseInfo.PurchaseDurationTotal
            );
        }

        public bool IsCurrentlyRunning()
        {
            if (!State.Purchases.Any()) return false;

            return State.Purchases.Any(p =>
                p.Submitted.HasValue &&
                p.Started.HasValue &&
                !p.Expiry.HasValue &&
                !p.Finish.HasValue &&
                p.Started.Value > DateTime.UtcNow - purchaseInfo.PurchaseDurationTotal
            );
        }

        public bool IsCurrentlyFailed()
        {
            if (!State.Purchases.Any()) return false;

            var mostRecent = GetMostRecent();
            if (mostRecent == null) return false;

            return mostRecent.Expiry.HasValue;
        }

        protected WorkerPurchase? GetMostRecent()
        {
            if (!State.Purchases.Any()) return null;
            var maxCreated = State.Purchases.Max(p => p.Created);
            return State.Purchases.SingleOrDefault(p => p.Created == maxCreated);
        }
    }
}
