using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

// Generated code, do not modify.
// See Marketplace/README for how to update.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace CodexContractsPlugin
{
    public partial class TestTokenDeployment : TestTokenDeploymentBase
    {
        public TestTokenDeployment() : base(BYTECODE) { }
        public TestTokenDeployment(string byteCode) : base(byteCode) { }
    }

    public class TestTokenDeploymentBase : ContractDeploymentMessage
    {
        public static string BYTECODE = "0x608060405234801561001057600080fd5b50604051806040016040528060098152602001682a32b9ba2a37b5b2b760b91b815250604051806040016040528060038152602001621514d560ea1b815250816003908161005e9190610112565b50600461006b8282610112565b5050506101d0565b634e487b7160e01b600052604160045260246000fd5b600181811c9082168061009d57607f821691505b6020821081036100bd57634e487b7160e01b600052602260045260246000fd5b50919050565b601f82111561010d57806000526020600020601f840160051c810160208510156100ea5750805b601f840160051c820191505b8181101561010a57600081556001016100f6565b50505b505050565b81516001600160401b0381111561012b5761012b610073565b61013f816101398454610089565b846100c3565b6020601f821160018114610173576000831561015b5750848201515b600019600385901b1c1916600184901b17845561010a565b600084815260208120601f198516915b828110156101a35787850151825560209485019460019092019101610183565b50848210156101c15786840151600019600387901b60f8161c191681555b50505050600190811b01905550565b610823806101df6000396000f3fe608060405234801561001057600080fd5b50600436106100be5760003560e01c806340c10f191161007657806395d89b411161005b57806395d89b4114610176578063a9059cbb1461017e578063dd62ed3e1461019157600080fd5b806340c10f191461013857806370a082311461014d57600080fd5b806318160ddd116100a757806318160ddd1461010457806323b872dd14610116578063313ce5671461012957600080fd5b806306fdde03146100c3578063095ea7b3146100e1575b600080fd5b6100cb6101ca565b6040516100d8919061066c565b60405180910390f35b6100f46100ef3660046106d6565b61025c565b60405190151581526020016100d8565b6002545b6040519081526020016100d8565b6100f4610124366004610700565b610276565b604051601281526020016100d8565b61014b6101463660046106d6565b61029a565b005b61010861015b36600461073d565b6001600160a01b031660009081526020819052604090205490565b6100cb6102a8565b6100f461018c3660046106d6565b6102b7565b61010861019f36600461075f565b6001600160a01b03918216600090815260016020908152604080832093909416825291909152205490565b6060600380546101d990610792565b80601f016020809104026020016040519081016040528092919081815260200182805461020590610792565b80156102525780601f1061022757610100808354040283529160200191610252565b820191906000526020600020905b81548152906001019060200180831161023557829003601f168201915b5050505050905090565b60003361026a8185856102c5565b60019150505b92915050565b6000336102848582856102d7565b61028f858585610374565b506001949350505050565b6102a482826103ec565b5050565b6060600480546101d990610792565b60003361026a818585610374565b6102d28383836001610422565b505050565b6001600160a01b0383811660009081526001602090815260408083209386168352929052205460001981101561036e578181101561035f576040517ffb8f41b20000000000000000000000000000000000000000000000000000000081526001600160a01b038416600482015260248101829052604481018390526064015b60405180910390fd5b61036e84848484036000610422565b50505050565b6001600160a01b0383166103b7576040517f96c6fd1e00000000000000000000000000000000000000000000000000000000815260006004820152602401610356565b6001600160a01b0382166103e15760405163ec442f0560e01b815260006004820152602401610356565b6102d2838383610529565b6001600160a01b0382166104165760405163ec442f0560e01b815260006004820152602401610356565b6102a460008383610529565b6001600160a01b038416610465576040517fe602df0500000000000000000000000000000000000000000000000000000000815260006004820152602401610356565b6001600160a01b0383166104a8576040517f94280d6200000000000000000000000000000000000000000000000000000000815260006004820152602401610356565b6001600160a01b038085166000908152600160209081526040808320938716835292905220829055801561036e57826001600160a01b0316846001600160a01b03167f8c5be1e5ebec7d5bd14f71427d1e84f3dd0314c0f7b2291e5b200ac8c7c3b9258460405161051b91815260200190565b60405180910390a350505050565b6001600160a01b03831661055457806002600082825461054991906107cc565b909155506105df9050565b6001600160a01b038316600090815260208190526040902054818110156105c0576040517fe450d38c0000000000000000000000000000000000000000000000000000000081526001600160a01b03851660048201526024810182905260448101839052606401610356565b6001600160a01b03841660009081526020819052604090209082900390555b6001600160a01b0382166105fb5760028054829003905561061a565b6001600160a01b03821660009081526020819052604090208054820190555b816001600160a01b0316836001600160a01b03167fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef8360405161065f91815260200190565b60405180910390a3505050565b602081526000825180602084015260005b8181101561069a576020818601810151604086840101520161067d565b506000604082850101526040601f19601f83011684010191505092915050565b80356001600160a01b03811681146106d157600080fd5b919050565b600080604083850312156106e957600080fd5b6106f2836106ba565b946020939093013593505050565b60008060006060848603121561071557600080fd5b61071e846106ba565b925061072c602085016106ba565b929592945050506040919091013590565b60006020828403121561074f57600080fd5b610758826106ba565b9392505050565b6000806040838503121561077257600080fd5b61077b836106ba565b9150610789602084016106ba565b90509250929050565b600181811c908216806107a657607f821691505b6020821081036107c657634e487b7160e01b600052602260045260246000fd5b50919050565b8082018082111561027057634e487b7160e01b600052601160045260246000fdfea26469706673582212209301276ee274d22ba81d0ea93fe7e3b411212420214338b4bbbd546fd54253a564736f6c634300081c0033";
        public TestTokenDeploymentBase() : base(BYTECODE) { }
        public TestTokenDeploymentBase(string byteCode) : base(byteCode) { }

    }

    public partial class AllowanceFunction : AllowanceFunctionBase { }

    [Function("allowance", "uint256")]
    public class AllowanceFunctionBase : FunctionMessage
    {
        [Parameter("address", "owner", 1)]
        public virtual string Owner { get; set; }
        [Parameter("address", "spender", 2)]
        public virtual string Spender { get; set; }
    }

    public partial class ApproveFunction : ApproveFunctionBase { }

    [Function("approve", "bool")]
    public class ApproveFunctionBase : FunctionMessage
    {
        [Parameter("address", "spender", 1)]
        public virtual string Spender { get; set; }
        [Parameter("uint256", "value", 2)]
        public virtual BigInteger Value { get; set; }
    }

    public partial class BalanceOfFunction : BalanceOfFunctionBase { }

    [Function("balanceOf", "uint256")]
    public class BalanceOfFunctionBase : FunctionMessage
    {
        [Parameter("address", "account", 1)]
        public virtual string Account { get; set; }
    }

    public partial class DecimalsFunction : DecimalsFunctionBase { }

    [Function("decimals", "uint8")]
    public class DecimalsFunctionBase : FunctionMessage
    {

    }

    public partial class MintFunction : MintFunctionBase { }

    [Function("mint")]
    public class MintFunctionBase : FunctionMessage
    {
        [Parameter("address", "holder", 1)]
        public virtual string Holder { get; set; }
        [Parameter("uint256", "amount", 2)]
        public virtual BigInteger Amount { get; set; }
    }

    public partial class NameFunction : NameFunctionBase { }

    [Function("name", "string")]
    public class NameFunctionBase : FunctionMessage
    {

    }

    public partial class SymbolFunction : SymbolFunctionBase { }

    [Function("symbol", "string")]
    public class SymbolFunctionBase : FunctionMessage
    {

    }

    public partial class TotalSupplyFunction : TotalSupplyFunctionBase { }

    [Function("totalSupply", "uint256")]
    public class TotalSupplyFunctionBase : FunctionMessage
    {

    }

    public partial class TransferFunction : TransferFunctionBase { }

    [Function("transfer", "bool")]
    public class TransferFunctionBase : FunctionMessage
    {
        [Parameter("address", "to", 1)]
        public virtual string To { get; set; }
        [Parameter("uint256", "value", 2)]
        public virtual BigInteger Value { get; set; }
    }

    public partial class TransferFromFunction : TransferFromFunctionBase { }

    [Function("transferFrom", "bool")]
    public class TransferFromFunctionBase : FunctionMessage
    {
        [Parameter("address", "from", 1)]
        public virtual string From { get; set; }
        [Parameter("address", "to", 2)]
        public virtual string To { get; set; }
        [Parameter("uint256", "value", 3)]
        public virtual BigInteger Value { get; set; }
    }

    public partial class ApprovalEventDTO : ApprovalEventDTOBase { }

    [Event("Approval")]
    public class ApprovalEventDTOBase : IEventDTO
    {
        [Parameter("address", "owner", 1, true)]
        public virtual string Owner { get; set; }
        [Parameter("address", "spender", 2, true)]
        public virtual string Spender { get; set; }
        [Parameter("uint256", "value", 3, false)]
        public virtual BigInteger Value { get; set; }
    }

    public partial class TransferEventDTO : TransferEventDTOBase { }

    [Event("Transfer")]
    public class TransferEventDTOBase : IEventDTO
    {
        [Parameter("address", "from", 1, true)]
        public virtual string From { get; set; }
        [Parameter("address", "to", 2, true)]
        public virtual string To { get; set; }
        [Parameter("uint256", "value", 3, false)]
        public virtual BigInteger Value { get; set; }
    }

    public partial class AllowanceOutputDTO : AllowanceOutputDTOBase { }

    [FunctionOutput]
    public class AllowanceOutputDTOBase : IFunctionOutputDTO
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }



    public partial class BalanceOfOutputDTO : BalanceOfOutputDTOBase { }

    [FunctionOutput]
    public class BalanceOfOutputDTOBase : IFunctionOutputDTO
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }

    public partial class DecimalsOutputDTO : DecimalsOutputDTOBase { }

    [FunctionOutput]
    public class DecimalsOutputDTOBase : IFunctionOutputDTO
    {
        [Parameter("uint8", "", 1)]
        public virtual byte ReturnValue1 { get; set; }
    }



    public partial class NameOutputDTO : NameOutputDTOBase { }

    [FunctionOutput]
    public class NameOutputDTOBase : IFunctionOutputDTO
    {
        [Parameter("string", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }

    public partial class SymbolOutputDTO : SymbolOutputDTOBase { }

    [FunctionOutput]
    public class SymbolOutputDTOBase : IFunctionOutputDTO
    {
        [Parameter("string", "", 1)]
        public virtual string ReturnValue1 { get; set; }
    }

    public partial class TotalSupplyOutputDTO : TotalSupplyOutputDTOBase { }

    [FunctionOutput]
    public class TotalSupplyOutputDTOBase : IFunctionOutputDTO
    {
        [Parameter("uint256", "", 1)]
        public virtual BigInteger ReturnValue1 { get; set; }
    }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
