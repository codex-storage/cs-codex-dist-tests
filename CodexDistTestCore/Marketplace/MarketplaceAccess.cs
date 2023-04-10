using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace CodexDistTestCore.Marketplace
{
    public interface IMarketplaceAccess
    {
        void AdvertiseStorage(ByteSize size, float pricePerMBPerSecond, float collateral);
        void AdvertiseContract(ContentId contentId, float maxPricePerMBPerSecond, float minRequiredCollateral, float minRequiredNumberOfDuplicates);
        void AssertThatBalance(IResolveConstraint constraint, string message = "");
        float GetBalance();
    }

    public class MarketplaceAccess : IMarketplaceAccess
    {
        public void AdvertiseContract(ContentId contentId, float maxPricePerMBPerSecond, float minRequiredCollateral, float minRequiredNumberOfDuplicates)
        {
            throw new NotImplementedException();
        }

        public void AdvertiseStorage(ByteSize size, float pricePerMBPerSecond, float collateral)
        {
            throw new NotImplementedException();
        }

        public void AssertThatBalance(IResolveConstraint constraint, string message = "")
        {
            throw new NotImplementedException();
        }

        public float GetBalance()
        {
            throw new NotImplementedException();
        }
    }

    public class MarketplaceUnavailable : IMarketplaceAccess
    {
        public void AdvertiseContract(ContentId contentId, float maxPricePerMBPerSecond, float minRequiredCollateral, float minRequiredNumberOfDuplicates)
        {
            Unavailable();
        }

        public void AdvertiseStorage(ByteSize size, float pricePerMBPerSecond, float collateral)
        {
            Unavailable();
        }

        public void AssertThatBalance(IResolveConstraint constraint, string message = "")
        {
            Unavailable();
        }

        public float GetBalance()
        {
            Unavailable();
            return 0.0f;
        }

        private void Unavailable()
        {
            Assert.Fail("Incorrect test setup: Marketplace was not enabled for this group of Codex nodes. Add 'EnableMarketplace(...)' after 'SetupCodexNodes()' to enable it.");
            throw new InvalidOperationException();
        }
    }
}
