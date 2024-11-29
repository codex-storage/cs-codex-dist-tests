using CodexContractsPlugin.Marketplace;
using GethPlugin;
using Logging;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using NethereumWorkflow;
using System.Numerics;

namespace CodexContractsPlugin
{
    public class ContractInteractions
    {
        private readonly ILog log;
        private readonly IGethNode gethNode;

        public ContractInteractions(ILog log, IGethNode gethNode)
        {
            this.log = log;
            this.gethNode = gethNode;
        }

        public string GetTokenAddress(string marketplaceAddress)
        {
            log.Debug(marketplaceAddress);
            var function = new TokenFunctionBase();
            return gethNode.Call<TokenFunctionBase, string>(marketplaceAddress, function);
        }

        public string GetTokenName(string tokenAddress)
        {
            try
            {
                log.Debug(tokenAddress);
                var function = new GetTokenNameFunction();

                return gethNode.Call<GetTokenNameFunction, string>(tokenAddress, function);
            }
            catch (Exception ex)
            {
                log.Log("Failed to get token name: " + ex);
                return string.Empty;
            }
        }

        public string MintTestTokens(EthAddress address, BigInteger amount, string tokenAddress)
        {
            log.Debug($"{amount} -> {address} (token: {tokenAddress})");
            return MintTokens(address.Address, amount, tokenAddress);
        }

        public decimal GetBalance(string tokenAddress, string account)
        {
            log.Debug($"({tokenAddress}) {account}");
            var function = new GetTokenBalanceFunction
            {
                Owner = account
            };

            return gethNode.Call<GetTokenBalanceFunction, BigInteger>(tokenAddress, function).ToDecimal();
        }

        public GetRequestOutputDTO GetRequest(string marketplaceAddress, byte[] requestId)
        {

            log.Debug($"({marketplaceAddress}) {requestId.ToHex(true)}");
            var func = new GetRequestFunction
            {
                RequestId = requestId
            };
            return gethNode.Call<GetRequestFunction, GetRequestOutputDTO>(marketplaceAddress, func);
        }

        public bool IsSynced(string marketplaceAddress, string marketplaceAbi)
        {
            log.Debug();
            try
            {
                return IsBlockNumberOK() && IsContractAvailable(marketplaceAddress, marketplaceAbi);
            }
            catch
            {
                return false;
            }
        }

        private string MintTokens(string account, BigInteger amount, string tokenAddress)
        {
            log.Debug($"({tokenAddress}) {amount} --> {account}");
            if (string.IsNullOrEmpty(account)) throw new ArgumentException("Invalid arguments for MintTestTokens");

            var function = new MintTokensFunction
            {
                Holder = account,
                Amount = amount
            };

            return gethNode.SendTransaction(tokenAddress, function);
        }

        private bool IsBlockNumberOK()
        {
            var n = gethNode.GetSyncedBlockNumber();
            return n != null && n > 256;
        }

        private bool IsContractAvailable(string marketplaceAddress, string marketplaceAbi)
        {
            return gethNode.IsContractAvailable(marketplaceAbi, marketplaceAddress);
        }
    }

    [Function("name", "string")]
    public class GetTokenNameFunction : FunctionMessage
    {
    }

    [Function("mint")]
    public class MintTokensFunction : FunctionMessage
    {
        [Parameter("address", "holder", 1)]
        public string Holder { get; set; } = string.Empty;

        [Parameter("uint256", "amount", 2)]
        public BigInteger Amount { get; set; }
    }

    [Function("balanceOf", "uint256")]
    public class GetTokenBalanceFunction : FunctionMessage
    {
        [Parameter("address", "owner", 1)]
        public string Owner { get; set; } = string.Empty;
    }
}
