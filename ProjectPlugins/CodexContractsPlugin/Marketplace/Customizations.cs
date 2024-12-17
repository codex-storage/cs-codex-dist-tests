#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using BlockchainUtils;
using GethPlugin;
using Newtonsoft.Json;

namespace CodexContractsPlugin.Marketplace
{
    public interface IHasBlock
    {
        BlockTimeEntry Block { get; set; }
    }

    public interface IHasRequestId
    {
        byte[] RequestId { get; set; }
    }

    public partial class Request : RequestBase, IHasBlock, IHasRequestId
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
        public byte[] RequestId { get; set; }

        public EthAddress ClientAddress { get { return new EthAddress(Client); } }

        [JsonIgnore]
        public string Id
        {
            get
            {
                return BitConverter.ToString(RequestId).Replace("-", "").ToLowerInvariant();
            }
        }
    }

    public partial class RequestFulfilledEventDTO : IHasBlock, IHasRequestId
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }

    public partial class RequestCancelledEventDTO : IHasBlock, IHasRequestId
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }

    public partial class RequestFailedEventDTO : IHasBlock, IHasRequestId
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }

    public partial class SlotFilledEventDTO : IHasBlock, IHasRequestId
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
        public EthAddress Host { get; set; }
    }

    public partial class SlotFreedEventDTO : IHasBlock, IHasRequestId
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }

    public partial class SlotReservationsFullEventDTO : IHasBlock, IHasRequestId
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
