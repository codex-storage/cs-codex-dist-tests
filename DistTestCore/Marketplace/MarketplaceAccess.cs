using DistTestCore.Codex;
using Logging;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using System.Numerics;
using Utils;

namespace DistTestCore.Marketplace
{
    public interface IMarketplaceAccess
    {
        string MakeStorageAvailable(ByteSize size, TestToken minPricePerBytePerSecond, TestToken maxCollateral, TimeSpan maxDuration);
        string RequestStorage(ContentId contentId, TestToken pricePerBytePerSecond, TestToken requiredCollateral, uint minRequiredNumberOfNodes, int proofProbability, TimeSpan duration);
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

        public string RequestStorage(ContentId contentId, TestToken pricePerBytePerSecond, TestToken requiredCollateral, uint minRequiredNumberOfNodes, int proofProbability, TimeSpan duration)
        {
            var request = new CodexSalesRequestStorageRequest
            {
                duration = ToHexBigInt(duration.TotalSeconds),
                proofProbability = ToHexBigInt(proofProbability),
                reward = ToHexBigInt(pricePerBytePerSecond),
                collateral = ToHexBigInt(requiredCollateral),
                expiry = null,
                nodes = minRequiredNumberOfNodes,
                tolerance = null,
            };

            Log($"Requesting storage for: {contentId.Id}... (" +
                $"pricePerBytePerSecond: {pricePerBytePerSecond}, " +
                $"requiredCollateral: {requiredCollateral}, " +
                $"minRequiredNumberOfNodes: {minRequiredNumberOfNodes}, " +
                $"proofProbability: {proofProbability}, " +
                $"duration: {Time.FormatDuration(duration)})");

            var response = codexAccess.RequestStorage(request, contentId.Id);

            if (response == "Purchasing not available")
            {
                throw new InvalidOperationException(response);
            }

            Log($"Storage requested successfully. PurchaseId: {response}");

            return response;
        }

        public string MakeStorageAvailable(ByteSize size, TestToken minPricePerBytePerSecond, TestToken maxCollateral, TimeSpan maxDuration)
        {
            var request = new CodexSalesAvailabilityRequest
            {
                size = ToHexBigInt(size.SizeInBytes),
                duration = ToHexBigInt(maxDuration.TotalSeconds),
                maxCollateral = ToHexBigInt(maxCollateral),
                minPrice = ToHexBigInt(minPricePerBytePerSecond)
            };

            Log($"Making storage available... (" +
                $"size: {size}, " +
                $"minPricePerBytePerSecond: {minPricePerBytePerSecond}, " +
                $"maxCollateral: {maxCollateral}, " +
                $"maxDuration: {Time.FormatDuration(maxDuration)})");

            var response = codexAccess.SalesAvailability(request);

            Log($"Storage successfully made available. Id: {response.id}");

            return response.id;
        }

        private string ToHexBigInt(double d)
        {
            return "0x" + string.Format("{0:X}", Convert.ToInt64(d));
        }

        public string ToHexBigInt(TestToken t)
        {
            var bigInt = new BigInteger(t.Amount);
            return "0x" + bigInt.ToString("X");
        }

        public void AssertThatBalance(IResolveConstraint constraint, string message = "")
        {
            Assert.That(GetBalance(), constraint, message);
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
