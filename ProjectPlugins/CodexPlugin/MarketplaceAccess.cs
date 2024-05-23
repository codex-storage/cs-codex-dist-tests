using Logging;
using Newtonsoft.Json;
using Utils;

namespace CodexPlugin
{
    public interface IMarketplaceAccess
    {
        string MakeStorageAvailable(StorageAvailability availability);
        StoragePurchaseContract RequestStorage(StoragePurchaseRequest purchase);
    }

    public class MarketplaceAccess : IMarketplaceAccess
    {
        private readonly ILog log;
        private readonly CodexAccess codexAccess;

        public MarketplaceAccess(ILog log, CodexAccess codexAccess)
        {
            this.log = log;
            this.codexAccess = codexAccess;
        }

        public StoragePurchaseContract RequestStorage(StoragePurchaseRequest purchase)
        {
            purchase.Log(log);

            var response = codexAccess.RequestStorage(purchase);

            if (string.IsNullOrEmpty(response) ||
                response == "Purchasing not available" ||
                response == "Expiry required" ||
                response == "Expiry needs to be in future" ||
                response == "Expiry has to be before the request's end (now + duration)")
            {
                throw new InvalidOperationException(response);
            }

            Log($"Storage requested successfully. PurchaseId: '{response}'.");

            var contract = new StoragePurchaseContract(log, codexAccess, response, purchase);
            contract.WaitForStorageContractSubmitted();
            return contract;
        }

        public string MakeStorageAvailable(StorageAvailability availability)
        {
            availability.Log(log);

            var response = codexAccess.SalesAvailability(availability);

            Log($"Storage successfully made available. Id: {response.Id}");

            return response.Id;
        }

        private void Log(string msg)
        {
            log.Log($"{codexAccess.Container.Containers.Single().Name} {msg}");
        }
    }

    public class MarketplaceUnavailable : IMarketplaceAccess
    {
        public string MakeStorageAvailable(StorageAvailability availability)
        {
            Unavailable();
            throw new NotImplementedException();
        }

        public StoragePurchaseContract RequestStorage(StoragePurchaseRequest purchase)
        {
            Unavailable();
            throw new NotImplementedException();
        }

        private void Unavailable()
        {
            FrameworkAssert.Fail("Incorrect test setup: Marketplace was not enabled for this group of Codex nodes. Add 'EnableMarketplace(...)' after 'SetupCodexNodes()' to enable it.");
            throw new InvalidOperationException();
        }
    }

    public class StoragePurchaseContract
    {
        private readonly ILog log;
        private readonly CodexAccess codexAccess;
        private readonly TimeSpan gracePeriod = TimeSpan.FromSeconds(30);
        private readonly DateTime contractPendingUtc = DateTime.UtcNow;
        private DateTime? contractSubmittedUtc = DateTime.UtcNow;
        private DateTime? contractStartedUtc;
        private DateTime? contractFinishedUtc;

        public StoragePurchaseContract(ILog log, CodexAccess codexAccess, string purchaseId, StoragePurchaseRequest purchase)
        {
            this.log = log;
            this.codexAccess = codexAccess;
            PurchaseId = purchaseId;
            Purchase = purchase;
        }

        public string PurchaseId { get; }
        public StoragePurchaseRequest Purchase { get; }

        public TimeSpan? PendingToSubmitted => contractSubmittedUtc - contractPendingUtc;
        public TimeSpan? SubmittedToStarted => contractStartedUtc - contractSubmittedUtc;
        public TimeSpan? SubmittedToFinished => contractFinishedUtc - contractSubmittedUtc;
        public TimeSpan? StartedToFinished => contractFinishedUtc - contractStartedUtc;

        public void WaitForStorageContractSubmitted()
        {
            WaitForStorageContractState(gracePeriod, "submitted", sleep: 200);
            contractSubmittedUtc = DateTime.UtcNow;
            LogSubmittedDuration();
        }

        public void WaitForStorageContractStarted()
        {
            var timeout = Purchase.Expiry + gracePeriod;

            WaitForStorageContractState(timeout, "started");
            contractStartedUtc = DateTime.UtcNow;
            LogStartedDuration();
        }

        public void WaitForStorageContractFinished()
        {
            if (!contractStartedUtc.HasValue)
            {
                WaitForStorageContractStarted();
            }
            var currentContractTime = DateTime.UtcNow - contractStartedUtc!.Value;
            var timeout = (Purchase.Duration - currentContractTime) + gracePeriod;
            WaitForStorageContractState(timeout, "finished");
            contractFinishedUtc = DateTime.UtcNow;
            LogFinishedDuration();
        }

        public StoragePurchase GetPurchaseStatus(string purchaseId)
        {
            return codexAccess.GetPurchaseStatus(purchaseId);
        }

        private void WaitForStorageContractState(TimeSpan timeout, string desiredState, int sleep = 1000)
        {
            var lastState = "";
            var waitStart = DateTime.UtcNow;

            Log($"Waiting for {Time.FormatDuration(timeout)} to reach state '{desiredState}'.");
            while (lastState != desiredState)
            {
                var purchaseStatus = codexAccess.GetPurchaseStatus(PurchaseId);
                var statusJson = JsonConvert.SerializeObject(purchaseStatus);
                if (purchaseStatus != null && purchaseStatus.State != lastState)
                {
                    lastState = purchaseStatus.State;
                    log.Debug("Purchase status: " + statusJson);
                }

                Thread.Sleep(sleep);

                if (lastState == "errored")
                {
                    FrameworkAssert.Fail("Contract errored: " + statusJson);
                }

                if (DateTime.UtcNow - waitStart > timeout)
                {
                    FrameworkAssert.Fail($"Contract did not reach '{desiredState}' within {Time.FormatDuration(timeout)} timeout. {statusJson}");
                }
            }
        }

        private void LogSubmittedDuration()
        {
            Log($"Pending to Submitted in {Time.FormatDuration(PendingToSubmitted)}");
        }

        private void LogStartedDuration()
        {
            Log($"Submitted to Started in {Time.FormatDuration(SubmittedToStarted)}");
        }

        private void LogFinishedDuration()
        {
            Log($"Submitted to Finished in {Time.FormatDuration(SubmittedToFinished)}");
            Log($"Started to Finished in {Time.FormatDuration(StartedToFinished)}");
        }

        private void Log(string msg)
        {
            log.Log($"[{PurchaseId}] {msg}");
        }
    }
}
