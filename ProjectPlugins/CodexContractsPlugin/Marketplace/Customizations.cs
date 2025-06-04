#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using BlockchainUtils;
using Nethereum.Hex.HexConvertors.Extensions;
using Newtonsoft.Json;
using CodexClient;
using Utils;

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

    public interface IHasSlotIndex
    {
        ulong SlotIndex { get; set; }
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

    public partial class SlotFilledEventDTO : IHasBlock, IHasRequestId, IHasSlotIndex
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
        public EthAddress Host { get; set; }

        public override string ToString()
        {
            return $"SlotFilled:[host:{Host} request:{RequestId.ToHex()} slotIndex:{SlotIndex}]";
        }
    }

    public partial class SlotFreedEventDTO : IHasBlock, IHasRequestId, IHasSlotIndex
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }

    public partial class SlotReservationsFullEventDTO : IHasBlock, IHasRequestId, IHasSlotIndex
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }

    public partial class ProofSubmittedEventDTO : IHasBlock
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }

    public partial class ReserveSlotFunction : IHasBlock, IHasRequestId, IHasSlotIndex
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }

    public partial class MarketplaceConfig : IMarketplaceConfigInput
    {

    }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
