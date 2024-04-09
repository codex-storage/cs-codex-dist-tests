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

            return new StoragePurchaseContract(log, codexAccess, response, purchase);
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
            log.Log($"{codexAccess.Container.Name} {msg}");
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
        private readonly TimeSpan gracePeriod = TimeSpan.FromSeconds(10);
        private DateTime? contractStartUtc;

        public StoragePurchaseContract(ILog log, CodexAccess codexAccess, string purchaseId, StoragePurchaseRequest purchase)
        {
            this.log = log;
            this.codexAccess = codexAccess;
            PurchaseId = purchaseId;
            Purchase = purchase;
        }

        public string PurchaseId { get; }
        public StoragePurchaseRequest Purchase { get; }

        public void WaitForStorageContractStarted()
        {
            var timeout = Purchase.Expiry + gracePeriod;

            WaitForStorageContractState(timeout, "started");
            contractStartUtc = DateTime.UtcNow;
        }

        public void WaitForStorageContractFinished()
        {
            if (!contractStartUtc.HasValue)
            {
                WaitForStorageContractStarted();
            }
            var currentContractTime = DateTime.UtcNow - contractStartUtc!.Value;
            var timeout = (Purchase.Duration - currentContractTime) + gracePeriod;
            WaitForStorageContractState(timeout, "finished");
        }

        public StoragePurchase GetPurchaseStatus(string purchaseId)
        {
            return codexAccess.GetPurchaseStatus(purchaseId);
        }

        private void WaitForStorageContractState(TimeSpan timeout, string desiredState)
        {
            var lastState = "";
            var waitStart = DateTime.UtcNow;

            log.Log($"Waiting for {Time.FormatDuration(timeout)} for contract '{PurchaseId}' to reach state '{desiredState}'.");
            while (lastState != desiredState)
            {
                var purchaseStatus = codexAccess.GetPurchaseStatus(PurchaseId);
                var statusJson = JsonConvert.SerializeObject(purchaseStatus);
                if (purchaseStatus != null && purchaseStatus.State != lastState)
                {
                    lastState = purchaseStatus.State;
                    log.Debug("Purchase status: " + statusJson);
                }

                Thread.Sleep(1000);

                if (lastState == "errored")
                {
                    FrameworkAssert.Fail("Contract errored: " + statusJson);
                }

                if (DateTime.UtcNow - waitStart > timeout)
                {
                    FrameworkAssert.Fail($"Contract did not reach '{desiredState}' within {Time.FormatDuration(timeout)} timeout. {statusJson}");
                }
            }
            log.Log($"Contract '{desiredState}'.");
        }
    }
}
