#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using GethPlugin;

namespace CodexContractsPlugin.Marketplace
{
    public partial class Request : RequestBase
    {
        public ulong BlockNumber { get; set; }
        public byte[] RequestId { get; set; }

        public EthAddress ClientAddress { get { return new EthAddress(Client); } }
    }

    public partial class SlotFilledEventDTO
    {
        public ulong BlockNumber { get; set; }
        public EthAddress Host { get; set; }
    }

    public partial class SlotFreedEventDTO
    {
        public ulong BlockNumber { get; set; }
    }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
