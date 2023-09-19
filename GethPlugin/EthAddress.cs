namespace GethPlugin
{
    public interface IEthAddress
    {
        string Address { get; }
    }

    public interface IHasEthAddress
    {
        IEthAddress EthAddress { get; }
    }

    public class EthAddress : IEthAddress
    {
        public EthAddress(string address)
        {
            Address = address;
        }

        public string Address { get; }
    }
}
