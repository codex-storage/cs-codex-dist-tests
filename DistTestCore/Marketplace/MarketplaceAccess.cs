using Logging;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace DistTestCore.Marketplace
{
    public interface IMarketplaceAccess
    {
        void MakeStorageAvailable(ByteSize size, int minPricePerBytePerSecond, float maxCollateral);
        void RequestStorage(ContentId contentId, int pricePerBytePerSecond, float requiredCollateral, float minRequiredNumberOfNodes);
        void AssertThatBalance(IResolveConstraint constraint, string message = "");
        decimal GetBalance();
    }

    public class MarketplaceAccess : IMarketplaceAccess
    {
        private readonly TestLog log;
        private readonly GethBootstrapNodeInfo bootstrapNode;
        private readonly GethCompanionNodeInfo companionNode;

        public MarketplaceAccess(TestLog log, GethBootstrapNodeInfo bootstrapNode, GethCompanionNodeInfo companionNode)
        {
            this.log = log;
            this.bootstrapNode = bootstrapNode;
            this.companionNode = companionNode;
        }

        public void RequestStorage(ContentId contentId, int pricePerBytePerSecond, float requiredCollateral, float minRequiredNumberOfNodes)
        {
            throw new NotImplementedException();
        }

        public void MakeStorageAvailable(ByteSize size, int minPricePerBytePerSecond, float maxCollateral)
        {
            throw new NotImplementedException();
        }

        public void AssertThatBalance(IResolveConstraint constraint, string message = "")
        {
            Assert.That(GetBalance(), constraint, message);
        }

        public decimal GetBalance()
        {
            var interaction = bootstrapNode.StartInteraction(log);
            return interaction.GetBalance(companionNode.Account);
        }
    }

    public class MarketplaceUnavailable : IMarketplaceAccess
    {
        public void RequestStorage(ContentId contentId, int pricePerBytePerSecond, float requiredCollateral, float minRequiredNumberOfNodes)
        {
            Unavailable();
        }

        public void MakeStorageAvailable(ByteSize size, int minPricePerBytePerSecond, float maxCollateral)
        {
            Unavailable();
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
