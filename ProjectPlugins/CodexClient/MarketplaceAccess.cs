﻿using CodexClient.Hooks;
using Logging;
using Utils;

namespace CodexClient
{
    public interface IMarketplaceAccess
    {
        string MakeStorageAvailable(CreateStorageAvailability availability);
        StorageAvailability[] GetAvailabilities();
        IStoragePurchaseContract RequestStorage(StoragePurchaseRequest purchase);
    }

    public class MarketplaceAccess : IMarketplaceAccess
    {
        private readonly ILog log;
        private readonly CodexAccess codexAccess;
        private readonly ICodexNodeHooks hooks;

        public MarketplaceAccess(ILog log, CodexAccess codexAccess, ICodexNodeHooks hooks)
        {
            this.log = log;
            this.codexAccess = codexAccess;
            this.hooks = hooks;
        }

        public IStoragePurchaseContract RequestStorage(StoragePurchaseRequest purchase)
        {
            purchase.Log(log);
            var swResult = Stopwatch.Measure(log, nameof(RequestStorage), () =>
            {
                return codexAccess.RequestStorage(purchase);
            });

            var response = swResult.Value;

            if (string.IsNullOrEmpty(response) ||
                response == "Unable to encode manifest" ||
                response == "Purchasing not available" ||
                response == "Expiry required" ||
                response == "Expiry needs to be in future" ||
                response == "Expiry has to be before the request's end (now + duration)")
            {
                throw new InvalidOperationException(response);
            }

            Log($"Storage requested successfully. PurchaseId: '{response}'.");

            return new StoragePurchaseContract(log, codexAccess, response, purchase, hooks);
        }

        public string MakeStorageAvailable(CreateStorageAvailability availability)
        {
            availability.Log(log);

            var response = codexAccess.SalesAvailability(availability);

            Log($"Storage successfully made available. Id: {response.Id}");
            hooks.OnStorageAvailabilityCreated(response);

            return response.Id;
        }

        public StorageAvailability[] GetAvailabilities()
        {
            var result = codexAccess.GetAvailabilities();
            Log($"Got {result.Length} availabilities:");
            foreach (var a in result) a.Log(log);
            return result;
        }

        private void Log(string msg)
        {
            log.Log($"{codexAccess.GetName()} {msg}");
        }
    }

    public class MarketplaceUnavailable : IMarketplaceAccess
    {
        public string MakeStorageAvailable(CreateStorageAvailability availability)
        {
            Unavailable();
            throw new NotImplementedException();
        }

        public IStoragePurchaseContract RequestStorage(StoragePurchaseRequest purchase)
        {
            Unavailable();
            throw new NotImplementedException();
        }

        public StorageAvailability[] GetAvailabilities()
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
