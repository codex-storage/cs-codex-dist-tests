using GethPlugin;

namespace CodexPlugin
{
    [Serializable]
    public class MarketplaceStartResults
    {
        public MarketplaceStartResults(EthAddress ethAddress, string privateKey)
        {
            EthAddress = ethAddress;
            PrivateKey = privateKey;
        }

        public EthAddress EthAddress { get; }
        public string PrivateKey { get; }
    }
}
