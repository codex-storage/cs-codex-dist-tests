namespace CodexPlugin
{
    [Serializable]
    public class MarketplaceStartResults
    {
        public MarketplaceStartResults(string ethAddress, string privateKey)
        {
            EthAddress = ethAddress;
            PrivateKey = privateKey;
        }

        public string EthAddress { get; }
        public string PrivateKey { get; }
    }
}
