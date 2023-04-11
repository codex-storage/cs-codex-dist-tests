using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace CodexDistTestCore.Marketplace
{
    public interface IMarketplaceAccess
    {
        void MakeStorageAvailable(ByteSize size, int minPricePerBytePerSecond, float maxCollateral);
        void RequestStorage(ContentId contentId, int pricePerBytePerSecond, float requiredCollateral, float minRequiredNumberOfNodes);
        void AssertThatBalance(IResolveConstraint constraint, string message = "");
        float GetBalance();
    }

    public class MarketplaceAccess : IMarketplaceAccess
    {
        private readonly K8sManager k8sManager;
        private readonly MarketplaceController marketplaceController;
        private readonly TestLog log;
        private readonly CodexNodeGroup group;
        private readonly GethCompanionNodeContainer gethCompanionNodeContainer;
        private string account = string.Empty;

        public MarketplaceAccess(
                                K8sManager k8sManager, 
                                MarketplaceController marketplaceController,
                                TestLog log,
                                CodexNodeGroup group, 
                                GethCompanionNodeContainer gethCompanionNodeContainer)
        {
            this.k8sManager = k8sManager;
            this.marketplaceController = marketplaceController;
            this.log = log;
            this.group = group;
            this.gethCompanionNodeContainer = gethCompanionNodeContainer;
        }

        public void Initialize()
        {
            EnsureAccount();

            marketplaceController.AddToBalance(account, group.Origin.MarketplaceConfig!.InitialBalance);

            log.Log($"Initialized Geth companion node with account '{account}' and initial balance {group.Origin.MarketplaceConfig!.InitialBalance}");
        }

        public void AdvertiseContract(ContentId contentId, float maxPricePerMBPerSecond, float minRequiredCollateral, float minRequiredNumberOfDuplicates)
        {
            throw new NotImplementedException();
        }

        public void MakeStorageAvailable(ByteSize size, float pricePerMBPerSecond, float collateral)
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

        private void EnsureAccount()
        {
            FetchAccount();
            if (string.IsNullOrEmpty(account))
            {
                Thread.Sleep(TimeSpan.FromSeconds(15));
                FetchAccount();
            }
            Assert.That(account, Is.Not.Empty, "Unable to fetch account for geth companion node. Test infra failure.");
        }

        private void FetchAccount()
        {
            account = k8sManager.ExecuteCommand(group.PodInfo!, gethCompanionNodeContainer.Name, "cat", GethDockerImage.AccountFilename);
        }
    }

    public class MarketplaceUnavailable : IMarketplaceAccess
    {
        public void AdvertiseContract(ContentId contentId, float maxPricePerMBPerSecond, float minRequiredCollateral, float minRequiredNumberOfDuplicates)
        {
            Unavailable();
        }

        public void MakeStorageAvailable(ByteSize size, float pricePerMBPerSecond, float collateral)
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
