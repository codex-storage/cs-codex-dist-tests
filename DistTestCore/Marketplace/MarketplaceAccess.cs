using DistTestCore.Codex;
using DistTestCore.Helpers;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using System.Numerics;
using Utils;

namespace DistTestCore.Marketplace
{
    public interface IMarketplaceAccess
    {
        string MakeStorageAvailable(ByteSize size, TestToken minPricePerBytePerSecond, TestToken maxCollateral, TimeSpan maxDuration);
        string RequestStorage(ContentId contentId, TestToken pricePerSlotPerSecond, TestToken requiredCollateral, uint minRequiredNumberOfNodes, int proofProbability, TimeSpan duration);
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

        public string RequestStorage(ContentId contentId, TestToken pricePerSlotPerSecond, TestToken requiredCollateral, uint minRequiredNumberOfNodes, int proofProbability, TimeSpan duration)
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

            var response = codexAccess.Node.RequestStorage(request, contentId.Id);

            if (response == "Purchasing not available")
            {
                throw new InvalidOperationException(response);
            }

            Log($"Storage requested successfully. PurchaseId: {response}");

            return response;
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
                $"minPricePerBytePerSecond: {minPriceForTotalSpace}, " +
                $"maxCollateral: {maxCollateral}, " +
                $"maxDuration: {Time.FormatDuration(maxDuration)})");

            var response = codexAccess.Node.SalesAvailability(request);

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
        public string RequestStorage(ContentId contentId, TestToken pricePerBytePerSecond, TestToken requiredCollateral, uint minRequiredNumberOfNodes, int proofProbability, TimeSpan duration)
        {
            Unavailable();
            return string.Empty;
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
}
