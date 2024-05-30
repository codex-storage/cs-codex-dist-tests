using Logging;
using Utils;

namespace CodexPlugin
{
    public interface IMarketplaceAccess
    {
        string MakeStorageAvailable(StorageAvailability availability);
        IStoragePurchaseContract RequestStorage(StoragePurchaseRequest purchase);
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

        public IStoragePurchaseContract RequestStorage(StoragePurchaseRequest purchase)
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

        public IStoragePurchaseContract RequestStorage(StoragePurchaseRequest purchase)
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
}
