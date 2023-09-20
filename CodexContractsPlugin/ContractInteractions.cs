using GethPlugin;
using Logging;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
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
            var function = new GetTokenFunction();

            return gethNode.Call<GetTokenFunction, string>(marketplaceAddress, function);
        }

        public void MintTestTokens(EthAddress address, decimal amount, string tokenAddress)
        {
            MintTokens(address.Address, amount, tokenAddress);
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

        public bool IsSynced(string marketplaceAddress, string marketplaceAbi)
        {
            try
            {
                return IsBlockNumberOK() && IsContractAvailable(marketplaceAddress, marketplaceAbi);
            }
            catch
            {
                return false;
            }
        }

        private void MintTokens(string account, decimal amount, string tokenAddress)
        {
            log.Debug($"({tokenAddress}) {amount} --> {account}");
            if (string.IsNullOrEmpty(account)) throw new ArgumentException("Invalid arguments for MintTestTokens");

            var function = new MintTokensFunction
            {
                Holder = account,
                Amount = amount.ToBig()
            };

            gethNode.SendTransaction(tokenAddress, function);
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

    [Function("token", "address")]
    public class GetTokenFunction : FunctionMessage
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
