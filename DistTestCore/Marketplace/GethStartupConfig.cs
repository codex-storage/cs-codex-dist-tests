namespace DistTestCore.Marketplace
{
    public class GethStartupConfig
    {
        public GethStartupConfig(bool isBootstrapNode, GethBootstrapNodeInfo bootstrapNode, int numberOfCompanionAccounts)
        {
            IsBootstrapNode = isBootstrapNode;
            BootstrapNode = bootstrapNode;
            NumberOfCompanionAccounts = numberOfCompanionAccounts;
        }

        public bool IsBootstrapNode { get; }
        public GethBootstrapNodeInfo BootstrapNode { get; }
        public int NumberOfCompanionAccounts { get; }
    }
}
