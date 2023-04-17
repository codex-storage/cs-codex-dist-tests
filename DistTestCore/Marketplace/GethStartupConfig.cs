namespace DistTestCore.Marketplace
{
    public class GethStartupConfig
    {
        public GethStartupConfig(bool isBootstrapNode, string genesisJsonBase64, GethBootstrapNodeInfo bootstrapNode)
        {
            IsBootstrapNode = isBootstrapNode;
            GenesisJsonBase64 = genesisJsonBase64;
            BootstrapNode = bootstrapNode;
        }

        public bool IsBootstrapNode { get; }
        public string GenesisJsonBase64 { get; }
        public GethBootstrapNodeInfo BootstrapNode { get; }
    }
}
