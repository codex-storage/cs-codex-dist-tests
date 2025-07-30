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

    public interface IHasBlockAndRequestId : IHasBlock, IHasRequestId
    {
    }

    public interface IHasSlotIndex
    {
        ulong SlotIndex { get; set; }
    }

    public partial class Request
    {
        public EthAddress ClientAddress { get { return new EthAddress(Client); } }
    }

    public partial class StorageRequestedEventDTO : IHasBlockAndRequestId
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }

    public partial class RequestFulfilledEventDTO : IHasBlockAndRequestId
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }

    public partial class RequestCancelledEventDTO : IHasBlockAndRequestId
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }

    public partial class RequestFailedEventDTO : IHasBlockAndRequestId
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }

    public partial class SlotFilledEventDTO : IHasBlockAndRequestId, IHasSlotIndex
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
        public EthAddress Host { get; set; }

        public override string ToString()
        {
            return $"SlotFilled:[host:{Host} request:{RequestId.ToHex()} slotIndex:{SlotIndex}]";
        }
    }

    public partial class SlotFreedEventDTO : IHasBlockAndRequestId, IHasSlotIndex
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }

    public partial class SlotReservationsFullEventDTO : IHasBlockAndRequestId, IHasSlotIndex
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }

    public partial class ProofSubmittedEventDTO : IHasBlock
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }

    public partial class ReserveSlotFunction : IHasBlockAndRequestId, IHasSlotIndex
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }

    public partial class MarketplaceConfig : IMarketplaceConfigInput
    {
        public int MaxNumberOfSlashes
        {
            get
            {
                if (Collateral == null) return -1;
                return Collateral.MaxNumberOfSlashes;
            }
        }

        public TimeSpan PeriodDuration
        {
            get
            {
                if (Proofs == null) return TimeSpan.MinValue;
                return TimeSpan.FromSeconds(this.Proofs.Period);
            }
        }
    }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
