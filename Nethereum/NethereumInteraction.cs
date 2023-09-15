using Logging;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using System.Numerics;
using Utils;

namespace NethereumWorkflow
{
    public class NethereumInteraction
    {
        private readonly ILog log;
        private readonly Web3 web3;

        internal NethereumInteraction(ILog log, Web3 web3)
        {
            this.log = log;
            this.web3 = web3;
        }

        public string GetTokenAddress(string marketplaceAddress)
        {
            log.Debug(marketplaceAddress);
            var function = new GetTokenFunction();

            var handler = web3.Eth.GetContractQueryHandler<GetTokenFunction>();
            return Time.Wait(handler.QueryAsync<string>(marketplaceAddress, function));
        }

        public void MintTestTokens(string[] accounts, decimal amount, string tokenAddress)
        {
            if (amount < 1 || accounts.Length < 1) throw new ArgumentException("Invalid arguments for MintTestTokens");

            var tasks = accounts.Select(a => MintTokens(a, amount, tokenAddress));

            Task.WaitAll(tasks.ToArray());
        }

        public decimal GetBalance(string tokenAddress, string account)
        {
            log.Debug($"({tokenAddress}) {account}");
            var function = new GetTokenBalanceFunction
            {
                Owner = account
            };

            var handler = web3.Eth.GetContractQueryHandler<GetTokenBalanceFunction>();
            return ToDecimal(Time.Wait(handler.QueryAsync<BigInteger>(tokenAddress, function)));
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

        private Task MintTokens(string account, decimal amount, string tokenAddress)
        {
            log.Debug($"({tokenAddress}) {amount} --> {account}");
            if (string.IsNullOrEmpty(account)) throw new ArgumentException("Invalid arguments for MintTestTokens");

            var function = new MintTokensFunction
            {
                Holder = account,
                Amount = ToBig(amount)
            };

            var handler = web3.Eth.GetContractTransactionHandler<MintTokensFunction>();
            return handler.SendRequestAndWaitForReceiptAsync(tokenAddress, function);
        }

        private bool IsBlockNumberOK()
        {
            log.Debug();
            var sync = Time.Wait(web3.Eth.Syncing.SendRequestAsync());
            var number = Time.Wait(web3.Eth.Blocks.GetBlockNumber.SendRequestAsync());
            var numberOfBlocks = ToDecimal(number);
            return !sync.IsSyncing && numberOfBlocks > 256;
        }

        private bool IsContractAvailable(string marketplaceAddress, string marketplaceAbi)
        {
            log.Debug();
            try
            {
                var contract = web3.Eth.GetContract(marketplaceAbi, marketplaceAddress);
                return contract != null;
            }
            catch
            {
                return false;
            }
        }

        private HexBigInteger ToHexBig(decimal amount)
        {
            var bigint = ToBig(amount);
            var str = bigint.ToString("X");
            return new HexBigInteger(str);
        }

        private BigInteger ToBig(decimal amount)
        {
            return new BigInteger(amount);
        }

        private decimal ToDecimal(HexBigInteger hexBigInteger)
        {
            return ToDecimal(hexBigInteger.Value);
        }

        private decimal ToDecimal(BigInteger bigInteger)
        {
            return (decimal)bigInteger;
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
