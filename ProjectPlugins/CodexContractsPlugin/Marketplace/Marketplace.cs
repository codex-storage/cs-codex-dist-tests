using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using System.Numerics;

// Generated code, do not modify.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace CodexContractsPlugin.Marketplace
{
    public partial class ConfigFunction : ConfigFunctionBase { }

    [Function("config", typeof(ConfigOutputDTO))]
    public class ConfigFunctionBase : FunctionMessage
    {

    }

    public partial class FillSlotFunction : FillSlotFunctionBase { }

    [Function("fillSlot")]
    public class FillSlotFunctionBase : FunctionMessage
    {
        [Parameter("bytes32", "requestId", 1)]
        public virtual byte[] RequestId { get; set; }
        [Parameter("uint256", "slotIndex", 2)]
        public virtual BigInteger SlotIndex { get; set; }
        [Parameter("bytes", "proof", 3)]
        public virtual byte[] Proof { get; set; }
    }

    public partial class FreeSlotFunction : FreeSlotFunctionBase { }

    [Function("freeSlot")]
    public class FreeSlotFunctionBase : FunctionMessage
    {
        [Parameter("bytes32", "slotId", 1)]
        public virtual byte[] SlotId { get; set; }
    }

    public partial class GetActiveSlotFunction : GetActiveSlotFunctionBase { }

    [Function("getActiveSlot", typeof(GetActiveSlotOutputDTO))]
    public class GetActiveSlotFunctionBase : FunctionMessage
    {
        [Parameter("bytes32", "slotId", 1)]
        public virtual byte[] SlotId { get; set; }
    }

    public partial class GetChallengeFunction : GetChallengeFunctionBase { }

    [Function("getChallenge", "bytes32")]
    public class GetChallengeFunctionBase : FunctionMessage
    {
        [Parameter("bytes32", "id", 1)]
        public virtual byte[] Id { get; set; }
    }

    public partial class GetHostFunction : GetHostFunctionBase { }

    [Function("getHost", "address")]
    public class GetHostFunctionBase : FunctionMessage
    {
        [Parameter("bytes32", "slotId", 1)]
        public virtual byte[] SlotId { get; set; }
    }

    public partial class GetPointerFunction : GetPointerFunctionBase { }

    [Function("getPointer", "uint8")]
    public class GetPointerFunctionBase : FunctionMessage
    {
        [Parameter("bytes32", "id", 1)]
        public virtual byte[] Id { get; set; }
    }

    public partial class GetRequestFunction : GetRequestFunctionBase { }

    [Function("getRequest", typeof(GetRequestOutputDTO))]
    public class GetRequestFunctionBase : FunctionMessage
    {
        [Parameter("bytes32", "requestId", 1)]
        public virtual byte[] RequestId { get; set; }
    }

    public partial class IsProofRequiredFunction : IsProofRequiredFunctionBase { }

    [Function("isProofRequired", "bool")]
    public class IsProofRequiredFunctionBase : FunctionMessage
    {
        [Parameter("bytes32", "id", 1)]
        public virtual byte[] Id { get; set; }
    }

    public partial class MarkProofAsMissingFunction : MarkProofAsMissingFunctionBase { }

    [Function("markProofAsMissing")]
    public class MarkProofAsMissingFunctionBase : FunctionMessage
    {
        [Parameter("bytes32", "slotId", 1)]
        public virtual byte[] SlotId { get; set; }
        [Parameter("uint256", "period", 2)]
        public virtual BigInteger Period { get; set; }
    }

    public partial class MissingProofsFunction : MissingProofsFunctionBase { }

    [Function("missingProofs", "uint256")]
    public class MissingProofsFunctionBase : FunctionMessage
    {
        [Parameter("bytes32", "slotId", 1)]
        public virtual byte[] SlotId { get; set; }
    }

    public partial class MyRequestsFunction : MyRequestsFunctionBase { }

    [Function("myRequests", "bytes32[]")]
    public class MyRequestsFunctionBase : FunctionMessage
    {

    }

    public partial class MySlotsFunction : MySlotsFunctionBase { }

    [Function("mySlots", "bytes32[]")]
    public class MySlotsFunctionBase : FunctionMessage
    {

    }

    public partial class RequestEndFunction : RequestEndFunctionBase { }

    [Function("requestEnd", "uint256")]
    public class RequestEndFunctionBase : FunctionMessage
    {
        [Parameter("bytes32", "requestId", 1)]
        public virtual byte[] RequestId { get; set; }
    }

    public partial class RequestStateFunction : RequestStateFunctionBase { }

    [Function("requestState", "uint8")]
    public class RequestStateFunctionBase : FunctionMessage
    {
        [Parameter("bytes32", "requestId", 1)]
        public virtual byte[] RequestId { get; set; }
    }

    public partial class RequestStorageFunction : RequestStorageFunctionBase { }

    [Function("requestStorage")]
    public class RequestStorageFunctionBase : FunctionMessage
    {
        [Parameter("tuple", "request", 1)]
        public virtual Request Request { get; set; }
    }

    public partial class SlotStateFunction : SlotStateFunctionBase { }

    [Function("slotState", "uint8")]
    public class SlotStateFunctionBase : FunctionMessage
    {
        [Parameter("bytes32", "slotId", 1)]
        public virtual byte[] SlotId { get; set; }
    }

    public partial class SubmitProofFunction : SubmitProofFunctionBase { }

    [Function("submitProof")]
    public class SubmitProofFunctionBase : FunctionMessage
    {
        [Parameter("bytes32", "id", 1)]
        public virtual byte[] Id { get; set; }
        [Parameter("bytes", "proof", 2)]
        public virtual byte[] Proof { get; set; }
    }

    public partial class TokenFunction : TokenFunctionBase { }

    [Function("token", "address")]
    public class TokenFunctionBase : FunctionMessage
    {

    }

    public partial class WillProofBeRequiredFunction : WillProofBeRequiredFunctionBase { }

    [Function("willProofBeRequired", "bool")]
    public class WillProofBeRequiredFunctionBase : FunctionMessage
    {
        [Parameter("bytes32", "id", 1)]
        public virtual byte[] Id { get; set; }
    }

    public partial class WithdrawFundsFunction : WithdrawFundsFunctionBase { }

    [Function("withdrawFunds")]
    public class WithdrawFundsFunctionBase : FunctionMessage
    {
        [Parameter("bytes32", "requestId", 1)]
        public virtual byte[] RequestId { get; set; }
    }

    public partial class ProofSubmittedEventDTO : ProofSubmittedEventDTOBase { }

    [Event("ProofSubmitted")]
    public class ProofSubmittedEventDTOBase : IEventDTO
    {
        [Parameter("bytes32", "id", 1, false)]
        public virtual byte[] Id { get; set; }
        [Parameter("bytes", "proof", 2, false)]
        public virtual byte[] Proof { get; set; }
    }

    public partial class RequestCancelledEventDTO : RequestCancelledEventDTOBase { }

    [Event("RequestCancelled")]
    public class RequestCancelledEventDTOBase : IEventDTO
    {
        [Parameter("bytes32", "requestId", 1, true)]
        public virtual byte[] RequestId { get; set; }
    }

    public partial class RequestFailedEventDTO : RequestFailedEventDTOBase { }

    [Event("RequestFailed")]
    public class RequestFailedEventDTOBase : IEventDTO
    {
        [Parameter("bytes32", "requestId", 1, true)]
        public virtual byte[] RequestId { get; set; }
    }

    public partial class RequestFulfilledEventDTO : RequestFulfilledEventDTOBase { }

    [Event("RequestFulfilled")]
    public class RequestFulfilledEventDTOBase : IEventDTO
    {
        [Parameter("bytes32", "requestId", 1, true)]
        public virtual byte[] RequestId { get; set; }
    }

    public partial class SlotFilledEventDTO : SlotFilledEventDTOBase { }

    [Event("SlotFilled")]
    public class SlotFilledEventDTOBase : IEventDTO
    {
        [Parameter("bytes32", "requestId", 1, true)]
        public virtual byte[] RequestId { get; set; }
        [Parameter("uint256", "slotIndex", 2, false)]
        public virtual BigInteger SlotIndex { get; set; }
    }

    public partial class SlotFreedEventDTO : SlotFreedEventDTOBase { }

    [Event("SlotFreed")]
    public class SlotFreedEventDTOBase : IEventDTO
    {
        [Parameter("bytes32", "requestId", 1, true)]
        public virtual byte[] RequestId { get; set; }
        [Parameter("uint256", "slotIndex", 2, false)]
        public virtual BigInteger SlotIndex { get; set; }
    }

    public partial class StorageRequestedEventDTO : StorageRequestedEventDTOBase { }

    [Event("StorageRequested")]
    public class StorageRequestedEventDTOBase : IEventDTO
    {
        [Parameter("bytes32", "requestId", 1, false)]
        public virtual byte[] RequestId { get; set; }
        [Parameter("tuple", "ask", 2, false)]
        public virtual Ask Ask { get; set; }
        [Parameter("uint256", "expiry", 3, false)]
        public virtual BigInteger Expiry { get; set; }
    }

    public partial class ConfigOutputDTO : ConfigOutputDTOBase { }

    [FunctionOutput]
    public class ConfigOutputDTOBase : IFunctionOutputDTO
    {
        [Parameter("tuple", "collateral", 1)]
        public virtual CollateralConfig Collateral { get; set; }
        [Parameter("tuple", "proofs", 2)]
        public virtual ProofConfig Proofs { get; set; }
    }





    public partial class GetActiveSlotOutputDTO : GetActiveSlotOutputDTOBase { }

    [FunctionOutput]
    public class GetActiveSlotOutputDTOBase : IFunctionOutputDTO
    {
        [Parameter("tuple", "", 1)]
        public virtual ActiveSlot ReturnValue1 { get; set; }
    }

    public partial class GetChallengeOutputDTO : GetChallengeOutputDTOBase { }

    [FunctionOutput]
    public class GetChallengeOutputDTOBase : IFunctionOutputDTO
    {
        [Parameter("bytes32", "", 1)]
        public virtual byte[] ReturnValue1 { get; set; }
    }

    public partial class GetHostOutputDTO : GetHostOutputDTOBase { }

    [FunctionOutput]
    public class GetHostOutputDTOBase : IFunctionOutputDTO
    {
        [Parameter("address", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }

    public partial class GetPointerOutputDTO : GetPointerOutputDTOBase { }

    [FunctionOutput]
    public class GetPointerOutputDTOBase : IFunctionOutputDTO
    {
        [Parameter("uint8", "", 1)]
        public virtual byte ReturnValue1 { get; set; }
    }

    public partial class GetRequestOutputDTO : GetRequestOutputDTOBase { }

    [FunctionOutput]
    public class GetRequestOutputDTOBase : IFunctionOutputDTO
    {
        [Parameter("tuple", "", 1)]
        public virtual Request ReturnValue1 { get; set; }
    }

    public partial class IsProofRequiredOutputDTO : IsProofRequiredOutputDTOBase { }

    [FunctionOutput]
    public class IsProofRequiredOutputDTOBase : IFunctionOutputDTO
    {
        [Parameter("bool", "", 1)]
        public virtual bool ReturnValue1 { get; set; }
    }



    public partial class MissingProofsOutputDTO : MissingProofsOutputDTOBase { }

    [FunctionOutput]
    public class MissingProofsOutputDTOBase : IFunctionOutputDTO
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }

    public partial class MyRequestsOutputDTO : MyRequestsOutputDTOBase { }

    [FunctionOutput]
    public class MyRequestsOutputDTOBase : IFunctionOutputDTO
    {
        [Parameter("bytes32[]", "", 1)]
        public virtual List<byte[]> ReturnValue1 { get; set; }
    }

    public partial class MySlotsOutputDTO : MySlotsOutputDTOBase { }

    [FunctionOutput]
    public class MySlotsOutputDTOBase : IFunctionOutputDTO
    {
        [Parameter("bytes32[]", "", 1)]
        public virtual List<byte[]> ReturnValue1 { get; set; }
    }

    public partial class RequestEndOutputDTO : RequestEndOutputDTOBase { }

    [FunctionOutput]
    public class RequestEndOutputDTOBase : IFunctionOutputDTO
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }

    public partial class RequestStateOutputDTO : RequestStateOutputDTOBase { }

    [FunctionOutput]
    public class RequestStateOutputDTOBase : IFunctionOutputDTO
    {
        [Parameter("uint8", "", 1)]
        public virtual byte ReturnValue1 { get; set; }
    }



    public partial class SlotStateOutputDTO : SlotStateOutputDTOBase { }

    [FunctionOutput]
    public class SlotStateOutputDTOBase : IFunctionOutputDTO
    {
        [Parameter("uint8", "", 1)]
        public virtual byte ReturnValue1 { get; set; }
    }



    public partial class TokenOutputDTO : TokenOutputDTOBase { }

    [FunctionOutput]
    public class TokenOutputDTOBase : IFunctionOutputDTO
    {
        [Parameter("address", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }

    public partial class WillProofBeRequiredOutputDTO : WillProofBeRequiredOutputDTOBase { }

    [FunctionOutput]
    public class WillProofBeRequiredOutputDTOBase : IFunctionOutputDTO
    {
        [Parameter("bool", "", 1)]
        public virtual bool ReturnValue1 { get; set; }
    }



    public partial class CollateralConfig : CollateralConfigBase { }

    public class CollateralConfigBase
    {
        [Parameter("uint8", "repairRewardPercentage", 1)]
        public virtual byte RepairRewardPercentage { get; set; }
        [Parameter("uint8", "maxNumberOfSlashes", 2)]
        public virtual byte MaxNumberOfSlashes { get; set; }
        [Parameter("uint16", "slashCriterion", 3)]
        public virtual ushort SlashCriterion { get; set; }
        [Parameter("uint8", "slashPercentage", 4)]
        public virtual byte SlashPercentage { get; set; }
    }

    public partial class ProofConfig : ProofConfigBase { }

    public class ProofConfigBase
    {
        [Parameter("uint256", "period", 1)]
        public virtual BigInteger Period { get; set; }
        [Parameter("uint256", "timeout", 2)]
        public virtual BigInteger Timeout { get; set; }
        [Parameter("uint8", "downtime", 3)]
        public virtual byte Downtime { get; set; }
    }

    public partial class MarketplaceConfig : MarketplaceConfigBase { }

    public class MarketplaceConfigBase
    {
        [Parameter("tuple", "collateral", 1)]
        public virtual CollateralConfig Collateral { get; set; }
        [Parameter("tuple", "proofs", 2)]
        public virtual ProofConfig Proofs { get; set; }
    }

    public partial class Ask : AskBase { }

    public class AskBase
    {
        [Parameter("uint64", "slots", 1)]
        public virtual ulong Slots { get; set; }
        [Parameter("uint256", "slotSize", 2)]
        public virtual BigInteger SlotSize { get; set; }
        [Parameter("uint256", "duration", 3)]
        public virtual BigInteger Duration { get; set; }
        [Parameter("uint256", "proofProbability", 4)]
        public virtual BigInteger ProofProbability { get; set; }
        [Parameter("uint256", "reward", 5)]
        public virtual BigInteger Reward { get; set; }
        [Parameter("uint256", "collateral", 6)]
        public virtual BigInteger Collateral { get; set; }
        [Parameter("uint64", "maxSlotLoss", 7)]
        public virtual ulong MaxSlotLoss { get; set; }
    }

    public partial class Content : ContentBase { }

    public class ContentBase
    {
        [Parameter("string", "cid", 1)]
        public virtual string Cid { get; set; }
        [Parameter("bytes32", "merkleRoot", 2)]
        public virtual byte[] MerkleRoot { get; set; }
    }

    public partial class Request : RequestBase { }

    public class RequestBase
    {
        [Parameter("address", "client", 1)]
        public virtual string Client { get; set; }
        [Parameter("tuple", "ask", 2)]
        public virtual Ask Ask { get; set; }
        [Parameter("tuple", "content", 3)]
        public virtual Content Content { get; set; }
        [Parameter("uint256", "expiry", 4)]
        public virtual BigInteger Expiry { get; set; }
        [Parameter("bytes32", "nonce", 5)]
        public virtual byte[] Nonce { get; set; }
    }

    public partial class ActiveSlot : ActiveSlotBase { }

    public class ActiveSlotBase
    {
        [Parameter("tuple", "request", 1)]
        public virtual Request Request { get; set; }
        [Parameter("uint256", "slotIndex", 2)]
        public virtual BigInteger SlotIndex { get; set; }
    }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
