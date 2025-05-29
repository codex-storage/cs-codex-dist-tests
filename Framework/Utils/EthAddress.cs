namespace Utils
{
    public interface IHasEthAddress
    {
        EthAddress EthAddress { get; }
    }

    [Serializable]
    public class EthAddress : IComparable<EthAddress>
    {
        public EthAddress(string address)
        {
            Address = address.ToLowerInvariant();
        }

        public string Address { get; }

        public int CompareTo(EthAddress? other)
        {
            return Address.CompareTo(other!.Address);
        }

        public override bool Equals(object? obj)
        {
            return obj is EthAddress token && Address == token.Address;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Address);
        }

        public override string ToString()
        {
            return Address;
        }

        public static bool operator ==(EthAddress a, EthAddress b)
        {
            return a.Address == b.Address;
        }

        public static bool operator !=(EthAddress a, EthAddress b)
        {
            return a.Address != b.Address;
        }
    }
}
