using GethPlugin;

namespace CodexPlugin
{
    [Serializable]
    public class MarketplaceStartResults
    {
        public MarketplaceStartResults(IEthAddress ethAddress, string privateKey)
        {
            EthAddress = ethAddress;
            PrivateKey = privateKey;
        }

        public IEthAddress EthAddress { get; }
        public string PrivateKey { get; }
    }
}
