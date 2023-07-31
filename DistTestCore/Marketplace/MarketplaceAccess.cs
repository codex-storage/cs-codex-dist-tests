using DistTestCore.Codex;
using DistTestCore.Helpers;
using Logging;
using Newtonsoft.Json;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using System.Numerics;
using Utils;

namespace DistTestCore.Marketplace
{
    public interface IMarketplaceAccess
    {
        string MakeStorageAvailable(ByteSize size, TestToken minPricePerBytePerSecond, TestToken maxCollateral, TimeSpan maxDuration);
        StoragePurchaseContract RequestStorage(ContentId contentId, TestToken pricePerSlotPerSecond, TestToken requiredCollateral, uint minRequiredNumberOfNodes, int proofProbability, TimeSpan duration);
        void AssertThatBalance(IResolveConstraint constraint, string message = "");
        TestToken GetBalance();
    }

    public class MarketplaceAccess : IMarketplaceAccess
    {
        private readonly TestLifecycle lifecycle;
        private readonly MarketplaceNetwork marketplaceNetwork;
        private readonly GethAccount account;
        private readonly CodexAccess codexAccess;

        public MarketplaceAccess(TestLifecycle lifecycle, MarketplaceNetwork marketplaceNetwork, GethAccount account, CodexAccess codexAccess)
        {
            this.lifecycle = lifecycle;
            this.marketplaceNetwork = marketplaceNetwork;
            this.account = account;
            this.codexAccess = codexAccess;
        }

        public StoragePurchaseContract RequestStorage(ContentId contentId, TestToken pricePerSlotPerSecond, TestToken requiredCollateral, uint minRequiredNumberOfNodes, int proofProbability, TimeSpan duration)
        {
            var request = new CodexSalesRequestStorageRequest
            {
                duration = ToDecInt(duration.TotalSeconds),
                proofProbability = ToDecInt(proofProbability),
                reward = ToDecInt(pricePerSlotPerSecond),
                collateral = ToDecInt(requiredCollateral),
                expiry = null,
                nodes = minRequiredNumberOfNodes,
                tolerance = null,
            };

            Log($"Requesting storage for: {contentId.Id}... (" +
                $"pricePerSlotPerSecond: {pricePerSlotPerSecond}, " +
                $"requiredCollateral: {requiredCollateral}, " +
                $"minRequiredNumberOfNodes: {minRequiredNumberOfNodes}, " +
                $"proofProbability: {proofProbability}, " +
                $"duration: {Time.FormatDuration(duration)})");

            var response = codexAccess.RequestStorage(request, contentId.Id);

            if (response == "Purchasing not available")
            {
                throw new InvalidOperationException(response);
            }

            Log($"Storage requested successfully. PurchaseId: '{response}'.");

            return new StoragePurchaseContract(lifecycle.Log, codexAccess, response, duration);
        }

        public string MakeStorageAvailable(ByteSize totalSpace, TestToken minPriceForTotalSpace, TestToken maxCollateral, TimeSpan maxDuration)
        {
            var request = new CodexSalesAvailabilityRequest
            {
                size = ToDecInt(totalSpace.SizeInBytes),
                duration = ToDecInt(maxDuration.TotalSeconds),
                maxCollateral = ToDecInt(maxCollateral),
                minPrice = ToDecInt(minPriceForTotalSpace)
            };

            Log($"Making storage available... (" +
                $"size: {totalSpace}, " +
                $"minPriceForTotalSpace: {minPriceForTotalSpace}, " +
                $"maxCollateral: {maxCollateral}, " +
                $"maxDuration: {Time.FormatDuration(maxDuration)})");

            var response = codexAccess.SalesAvailability(request);

            Log($"Storage successfully made available. Id: {response.id}");

            return response.id;
        }

        private string ToDecInt(double d)
        {
            var i = new BigInteger(d);
            return i.ToString("D");
        }

        public string ToDecInt(TestToken t)
        {
            var i = new BigInteger(t.Amount);
            return i.ToString("D");
        }

        public void AssertThatBalance(IResolveConstraint constraint, string message = "")
        {
            AssertHelpers.RetryAssert(constraint, GetBalance, message);
        }

        public TestToken GetBalance()
        {
            var interaction = marketplaceNetwork.StartInteraction(lifecycle);
            var amount = interaction.GetBalance(marketplaceNetwork.Marketplace.TokenAddress, account.Account);
            var balance = new TestToken(amount);

            Log($"Balance of {account.Account} is {balance}.");

            return balance;
        }

        private void Log(string msg)
        {
            lifecycle.Log.Log($"{codexAccess.Container.Name} {msg}");
        }
    }

    public class MarketplaceUnavailable : IMarketplaceAccess
    {
        public StoragePurchaseContract RequestStorage(ContentId contentId, TestToken pricePerBytePerSecond, TestToken requiredCollateral, uint minRequiredNumberOfNodes, int proofProbability, TimeSpan duration)
        {
            Unavailable();
            return null!;
        }

        public string MakeStorageAvailable(ByteSize size, TestToken minPricePerBytePerSecond, TestToken maxCollateral, TimeSpan duration)
        {
            Unavailable();
            return string.Empty;
        }

        public void AssertThatBalance(IResolveConstraint constraint, string message = "")
        {
            Unavailable();
        }

        public TestToken GetBalance()
        {
            Unavailable();
            return new TestToken(0);
        }

        private void Unavailable()
        {
            Assert.Fail("Incorrect test setup: Marketplace was not enabled for this group of Codex nodes. Add 'EnableMarketplace(...)' after 'SetupCodexNodes()' to enable it.");
            throw new InvalidOperationException();
        }
    }

    public class StoragePurchaseContract
    {
        private readonly BaseLog log;
        private readonly CodexAccess codexAccess;
        private DateTime? contractStartUtc;

        public StoragePurchaseContract(BaseLog log, CodexAccess codexAccess, string purchaseId, TimeSpan contractDuration)
        {
            this.log = log;
            this.codexAccess = codexAccess;
            PurchaseId = purchaseId;
            ContractDuration = contractDuration;
        }

        public string PurchaseId { get; }
        public TimeSpan ContractDuration { get; }

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
            var timeout = (ContractDuration - currentContractTime) + gracePeriod;
            WaitForStorageContractState(timeout, "finished");
        }

        /// <summary>
        /// Wait for contract to start. Max timeout depends on contract filesize. Allows more time for larger files.
        /// </summary>
        public void WaitForStorageContractStarted(ByteSize contractFileSize)
        {
            var filesizeInMb = contractFileSize.SizeInBytes / (1024 * 1024);
            var maxWaitTime = TimeSpan.FromSeconds(filesizeInMb * 10.0);

            WaitForStorageContractStarted(maxWaitTime);
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
                    Assert.Fail("Contract errored: " + statusJson);
                }

                if (DateTime.UtcNow - waitStart > timeout)
                {
                    Assert.Fail($"Contract did not reach '{desiredState}' within timeout. {statusJson}");
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
