using DistTestCore.Codex;
using Logging;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using System.Numerics;

namespace DistTestCore.Marketplace
{
    public interface IMarketplaceAccess
    {
        string MakeStorageAvailable(ByteSize size, TestToken minPricePerBytePerSecond, TestToken maxCollateral, TimeSpan maxDuration);
        string RequestStorage(ContentId contentId, TestToken pricePerBytePerSecond, TestToken requiredCollateral, uint minRequiredNumberOfNodes, int proofProbability, TimeSpan duration);
        void AssertThatBalance(IResolveConstraint constraint, string message = "");
        decimal GetBalance();
    }

    public class MarketplaceAccess : IMarketplaceAccess
    {
        private readonly TestLog log;
        private readonly MarketplaceNetwork marketplaceNetwork;
        private readonly GethCompanionNodeInfo companionNode;
        private readonly CodexAccess codexAccess;

        public MarketplaceAccess(TestLog log, MarketplaceNetwork marketplaceNetwork, GethCompanionNodeInfo companionNode, CodexAccess codexAccess)
        {
            this.log = log;
            this.marketplaceNetwork = marketplaceNetwork;
            this.companionNode = companionNode;
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

            var response = codexAccess.RequestStorage(request, contentId.Id);

            return response.purchaseId;
        }

        public string MakeStorageAvailable(ByteSize size, TestToken minPricePerBytePerSecond, TestToken maxCollateral, TimeSpan duration)
        {
            var request = new CodexSalesAvailabilityRequest
            {
                size = ToHexBigInt(size.SizeInBytes),
                duration = ToHexBigInt(duration.TotalSeconds),
                maxCollateral = ToHexBigInt(maxCollateral),
                minPrice = ToHexBigInt(minPricePerBytePerSecond)
            };

            var response = codexAccess.SalesAvailability(request);

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

        public decimal GetBalance()
        {
            var interaction = marketplaceNetwork.StartInteraction(log);
            return interaction.GetBalance(marketplaceNetwork.Marketplace.TokenAddress, companionNode.Account);
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

        public decimal GetBalance()
        {
            Unavailable();
            return 0;
        }

        private void Unavailable()
        {
            Assert.Fail("Incorrect test setup: Marketplace was not enabled for this group of Codex nodes. Add 'EnableMarketplace(...)' after 'SetupCodexNodes()' to enable it.");
            throw new InvalidOperationException();
        }
    }
}
