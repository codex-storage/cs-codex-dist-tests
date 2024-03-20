using CodexContractsPlugin;
using Logging;
using Newtonsoft.Json;
using Utils;

namespace CodexPlugin
{
    public interface IMarketplaceAccess
    {
        string MakeStorageAvailable(StorageAvailability availability);
        StoragePurchaseContract RequestStorage(StoragePurchase purchase);
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

        public StoragePurchaseContract RequestStorage(StoragePurchase purchase)
        {
            purchase.Log(log);
            var request = purchase.ToApiRequest();

            var response = codexAccess.RequestStorage(request, purchase.ContentId.Id);

            if (response == "Purchasing not available" || 
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
            var request = availability.ToApiRequest();

            var response = codexAccess.SalesAvailability(request);

            Log($"Storage successfully made available. Id: {response.id}");

            return response.id;
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

        public StoragePurchaseContract RequestStorage(StoragePurchase purchase)
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
        private DateTime? contractStartUtc;

        public StoragePurchaseContract(ILog log, CodexAccess codexAccess, string purchaseId, StoragePurchase purchase)
        {
            this.log = log;
            this.codexAccess = codexAccess;
            PurchaseId = purchaseId;
            Purchase = purchase;
        }

        public string PurchaseId { get; }
        public StoragePurchase Purchase { get; }

        public void WaitForStorageContractStarted()
        {
            WaitForStorageContractStarted(TimeSpan.FromSeconds(30));
        }

        public void WaitForStorageContractFinished()
        {
            if (!contractStartUtc.HasValue)
            {
                WaitForStorageContractStarted();
            }
            var gracePeriod = TimeSpan.FromSeconds(10);
            var currentContractTime = DateTime.UtcNow - contractStartUtc!.Value;
            var timeout = (Purchase.Duration - currentContractTime) + gracePeriod;
            WaitForStorageContractState(timeout, "finished");
        }

        public void WaitForStorageContractFinished(ByteSize contractFileSize)
        {
            if (!contractStartUtc.HasValue)
            {
                WaitForStorageContractStarted(contractFileSize.ToTimeSpan());
            }
            var gracePeriod = TimeSpan.FromSeconds(10);
            var currentContractTime = DateTime.UtcNow - contractStartUtc!.Value;
            var timeout = (Purchase.Duration - currentContractTime) + gracePeriod;
            WaitForStorageContractState(timeout, "finished");
        }

        public void WaitForStorageContractStarted(TimeSpan timeout)
        {
            WaitForStorageContractState(timeout, "started");
            contractStartUtc = DateTime.UtcNow;
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
                if (purchaseStatus != null && purchaseStatus.state != lastState)
                {
                    lastState = purchaseStatus.state;
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

        public CodexStoragePurchase GetPurchaseStatus(string purchaseId)
        {
            return codexAccess.GetPurchaseStatus(purchaseId);
        }
    }
}
