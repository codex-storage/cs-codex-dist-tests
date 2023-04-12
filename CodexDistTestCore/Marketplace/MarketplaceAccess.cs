using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace CodexDistTestCore.Marketplace
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
        private readonly K8sManager k8sManager;
        private readonly MarketplaceController marketplaceController;
        private readonly TestLog log;
        private readonly CodexNodeGroup group;
        private readonly GethCompanionNodeContainer container;

        public MarketplaceAccess(
                                K8sManager k8sManager, 
                                MarketplaceController marketplaceController,
                                TestLog log,
                                CodexNodeGroup group,
                                GethCompanionNodeContainer container)
        {
            this.k8sManager = k8sManager;
            this.marketplaceController = marketplaceController;
            this.log = log;
            this.group = group;
            this.container = container;
        }

        public void Initialize()
        {
            EnsureAccount();

            marketplaceController.AddToBalance(container.Account, group.Origin.MarketplaceConfig!.InitialBalance);

            log.Log($"Initialized Geth companion node with account '{container.Account}' and initial balance {group.Origin.MarketplaceConfig!.InitialBalance}");
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
            throw new NotImplementedException();
        }

        public decimal GetBalance()
        {
            return marketplaceController.GetBalance(container.Account);
        }

        private void EnsureAccount()
        {
            FetchAccount();
            if (string.IsNullOrEmpty(container.Account))
            {
                Thread.Sleep(TimeSpan.FromSeconds(15));
                FetchAccount();
            }
            Assert.That(container.Account, Is.Not.Empty, "Unable to fetch account for geth companion node. Test infra failure.");
        }

        private void FetchAccount()
        {
            container.Account = k8sManager.ExecuteCommand(group.GethCompanionGroup!.Pod!, container.Name, "cat", GethDockerImage.AccountFilename);
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
