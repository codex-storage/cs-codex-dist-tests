namespace Utils
{
    [Serializable]
    public class EthAccount
    {
        public EthAccount(EthAddress ethAddress, string privateKey)
        {
            EthAddress = ethAddress;
            PrivateKey = privateKey;
        }

        public EthAddress EthAddress { get; }
        public string PrivateKey { get; }

        public override string ToString()
        {
            return EthAddress.ToString();
        }
    }
}
