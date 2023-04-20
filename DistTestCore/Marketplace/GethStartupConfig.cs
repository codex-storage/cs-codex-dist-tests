namespace DistTestCore.Marketplace
{
    public class GethStartupConfig
    {
        public GethStartupConfig(bool isBootstrapNode, GethBootstrapNodeInfo bootstrapNode)
        {
            IsBootstrapNode = isBootstrapNode;
            BootstrapNode = bootstrapNode;
        }

        public bool IsBootstrapNode { get; }
        public GethBootstrapNodeInfo BootstrapNode { get; }
    }
}
