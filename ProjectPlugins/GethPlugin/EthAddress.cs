namespace GethPlugin
{
    public interface IHasEthAddress
    {
        EthAddress EthAddress { get; }
    }

    public class EthAddress
    {
        public EthAddress(string address)
        {
            Address = address;
        }

        public string Address { get; }

        public override bool Equals(object? obj)
        {
            return obj is EthAddress address &&
                   Address == address.Address;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Address);
        }

        public override string ToString()
        {
            return Address;
        }
    }
}
