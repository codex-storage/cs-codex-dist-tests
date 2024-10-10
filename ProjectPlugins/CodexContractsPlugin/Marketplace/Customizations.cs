#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using GethPlugin;
using NethereumWorkflow.BlockUtils;
using Newtonsoft.Json;

namespace CodexContractsPlugin.Marketplace
{
    public interface IHasBlock
    {
        BlockTimeEntry Block { get; set; }
    }

    public partial class Request : RequestBase, IHasBlock
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

    public partial class RequestFulfilledEventDTO : IHasBlock
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }

    public partial class RequestCancelledEventDTO : IHasBlock
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }

    public partial class RequestFailedEventDTO : IHasBlock
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }

    public partial class SlotFilledEventDTO : IHasBlock
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
        public EthAddress Host { get; set; }
    }

    public partial class SlotFreedEventDTO : IHasBlock
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }

    public partial class SlotReservationsFullEventDTO : IHasBlock
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
