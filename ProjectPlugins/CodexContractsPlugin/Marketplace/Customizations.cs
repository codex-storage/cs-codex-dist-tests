#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using GethPlugin;
using Newtonsoft.Json;

namespace CodexContractsPlugin.Marketplace
{
    public partial class Request : RequestBase
    {
        [JsonIgnore]
        public ulong BlockNumber { get; set; }
        public byte[] RequestId { get; set; }

        public EthAddress ClientAddress { get { return new EthAddress(Client); } }
    }

    public partial class RequestFulfilledEventDTO
    {
        [JsonIgnore]
        public ulong BlockNumber { get; set; }
    }

    public partial class RequestCancelledEventDTO
    {
        [JsonIgnore]
        public ulong BlockNumber { get; set; }
    }

    public partial class SlotFilledEventDTO
    {
        [JsonIgnore]
        public ulong BlockNumber { get; set; }
        public EthAddress Host { get; set; }
    }

    public partial class SlotFreedEventDTO
    {
        [JsonIgnore]
        public ulong BlockNumber { get; set; }
    }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
