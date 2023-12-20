#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using GethPlugin;

namespace CodexContractsPlugin.Marketplace
{
    public partial class Request : RequestBase
    {
        public byte[] RequestId { get; set; }

        public EthAddress ClientAddress { get { return new EthAddress(Client); } }
    }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
