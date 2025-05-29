namespace Utils
{
    [Serializable]
    public class EthAccount : IComparable<EthAccount>
    {
        public EthAccount(EthAddress ethAddress, string privateKey)
        {
            EthAddress = ethAddress;
            PrivateKey = privateKey;
        }

        public EthAddress EthAddress { get; }
        public string PrivateKey { get; }

        public int CompareTo(EthAccount? other)
        {
            return PrivateKey.CompareTo(other!.PrivateKey);
        }

        public override bool Equals(object? obj)
        {
            return obj is EthAccount token && PrivateKey == token.PrivateKey;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PrivateKey);
        }

        public override string ToString()
        {
            return EthAddress.ToString();
        }

        public static bool operator ==(EthAccount a, EthAccount b)
        {
            return a.PrivateKey == b.PrivateKey;
        }

        public static bool operator !=(EthAccount a, EthAccount b)
        {
            return a.PrivateKey != b.PrivateKey;
        }
    }
}
