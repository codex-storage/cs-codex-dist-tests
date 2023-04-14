namespace DistTestCore.Marketplace
{
    public class GethStartupConfig
    {
        public GethStartupConfig(bool isBootstrapNode, string genesisJsonBase64)
        {
            IsBootstrapNode = isBootstrapNode;
            GenesisJsonBase64 = genesisJsonBase64;
        }

        public bool IsBootstrapNode { get; }
        public string GenesisJsonBase64 { get; }
    }
}
