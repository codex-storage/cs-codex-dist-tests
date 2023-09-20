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
    }
}
