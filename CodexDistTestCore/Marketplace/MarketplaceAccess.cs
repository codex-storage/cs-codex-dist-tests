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
        private readonly K8sManager k8sManager;
        private readonly TestLog log;
        private string account = string.Empty;

        public MarketplaceAccess(K8sManager k8sManager, TestLog log)
        {
            this.k8sManager = k8sManager;
            this.log = log;
        }

        public void Initialize(PodInfo pod, GethCompanionNodeContainer gethCompanionNodeContainer)
        {
            FetchAccount(pod, gethCompanionNodeContainer);
            if (string.IsNullOrEmpty(account))
            {
                Thread.Sleep(TimeSpan.FromSeconds(15));
                FetchAccount(pod, gethCompanionNodeContainer);
            }
            Assert.That(account, Is.Not.Empty, "Unable to fetch account for geth companion node. Test infra failure.");
            log.Log($"Initialized Geth companion node with account '{account}'");
        }

        private void FetchAccount(PodInfo pod, GethCompanionNodeContainer gethCompanionNodeContainer)
        {
            account = k8sManager.ExecuteCommand(pod, gethCompanionNodeContainer.Name, "cat", GethDockerImage.AccountFilename);
        }

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
