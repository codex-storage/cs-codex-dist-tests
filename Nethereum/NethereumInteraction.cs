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
        private readonly List<Task> openTasks = new List<Task>();
        private readonly BaseLog log;
        private readonly Web3 web3;
        private readonly string rootAccount;

        internal NethereumInteraction(BaseLog log, Web3 web3, string rootAccount)
        {
            this.log = log;
            this.web3 = web3;
            this.rootAccount = rootAccount;
        }

        public string GetTokenAddress(string marketplaceAddress)
        {
            log.Debug(marketplaceAddress);
            var function = new GetTokenFunction();

            var handler = web3.Eth.GetContractQueryHandler<GetTokenFunction>();
            return Time.Wait(handler.QueryAsync<string>(marketplaceAddress, function));
        }

        public void TransferWeiTo(string account, decimal amount)
        {
            log.Debug($"{amount} --> {account}");
            if (amount < 1 || string.IsNullOrEmpty(account)) throw new ArgumentException("Invalid arguments for AddToBalance");

            var value = ToHexBig(amount);
            var transactionId = Time.Wait(web3.Eth.TransactionManager.SendTransactionAsync(rootAccount, account, value));
            openTasks.Add(web3.Eth.TransactionManager.TransactionReceiptService.PollForReceiptAsync(transactionId));
        }

        public void MintTestTokens(string account, decimal amount, string tokenAddress)
        {
            log.Debug($"({tokenAddress}) {amount} --> {account}");
            if (amount < 1 || string.IsNullOrEmpty(account)) throw new ArgumentException("Invalid arguments for MintTestTokens");

            var function = new MintTokensFunction
            {
                Holder = account,
                Amount = ToBig(amount)
            };

            var handler = web3.Eth.GetContractTransactionHandler<MintTokensFunction>();
            openTasks.Add(handler.SendRequestAndWaitForReceiptAsync(tokenAddress, function));
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

        public void WaitForAllTransactions()
        {
            var tasks = openTasks.ToArray();
            openTasks.Clear();

            Task.WaitAll(tasks);
        }

        public void EnsureSynced(string marketplaceAddress, string marketplaceAbi)
        {
            WaitUntilSynced();
            WaitForContract(marketplaceAddress, marketplaceAbi);
        }

        private void WaitUntilSynced()
        {
            log.Debug();
            Time.WaitUntil(() =>
            {
                var sync = Time.Wait(web3.Eth.Syncing.SendRequestAsync());
                var number = Time.Wait(web3.Eth.Blocks.GetBlockNumber.SendRequestAsync());
                var numberOfBlocks = ToDecimal(number);
                return !sync.IsSyncing && numberOfBlocks > 256;

            }, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(1));
        }

        private void WaitForContract(string marketplaceAddress, string marketplaceAbi)
        {
            log.Debug();
            Time.WaitUntil(() =>
            {
                try
                {
                    var contract = web3.Eth.GetContract(marketplaceAbi, marketplaceAddress);
                    return contract != null;
                }
                catch
                {
                    return false;
                }
            }, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(1));
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
        public string Holder { get; set; }

        [Parameter("uint256", "amount", 2)]
        public BigInteger Amount { get; set; }
    }

    [Function("balanceOf", "uint256")]
    public class GetTokenBalanceFunction : FunctionMessage
    {
        [Parameter("address", "owner", 1)]
        public string Owner { get; set; }
    }
}
