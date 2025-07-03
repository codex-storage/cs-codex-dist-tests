using BlockchainUtils;
using CodexContractsPlugin.Marketplace;
using GethPlugin;
using Logging;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Numerics;
using Utils;

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
                var function = new NameFunction();

                return gethNode.Call<NameFunction, string>(tokenAddress, function);
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
            var function = new BalanceOfFunction
            {
                Account = account
            };

            return gethNode.Call<BalanceOfFunction, BigInteger>(tokenAddress, function).ToDecimal();
        }

        public void TransferTestTokens(string tokenAddress, string toAccount, BigInteger amount)
        {
            log.Debug($"({tokenAddress}) {toAccount} {amount}");
            var function = new TransferFunction
            {
                To = toAccount,
                Value = amount
            };

            gethNode.SendTransaction(tokenAddress, function);
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

            var function = new MintFunction
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
}
