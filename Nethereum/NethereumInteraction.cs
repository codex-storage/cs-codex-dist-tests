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
        private readonly TestLog log;
        private readonly Web3 web3;
        private readonly string rootAccount;

        internal NethereumInteraction(TestLog log, Web3 web3, string rootAccount)
        {
            this.log = log;
            this.web3 = web3;
            this.rootAccount = rootAccount;
        }

        public string GetTokenAddress(string marketplaceAddress)
        {
            var function = new GetTokenFunction();

            var handler = web3.Eth.GetContractQueryHandler<GetTokenFunction>();
            return Time.Wait(handler.QueryAsync<string>(marketplaceAddress, function));
        }

        public void TransferWeiTo(string account, decimal amount)
        {
            if (amount < 1 || string.IsNullOrEmpty(account)) throw new ArgumentException("Invalid arguments for AddToBalance");

            var value = ToHexBig(amount);
            var transactionId = Time.Wait(web3.Eth.TransactionManager.SendTransactionAsync(rootAccount, account, value));
            openTasks.Add(web3.Eth.TransactionManager.TransactionReceiptService.PollForReceiptAsync(transactionId));
        }

        public void MintTestTokens(string account, decimal amount, string tokenAddress)
        {
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
            Time.WaitUntil(() =>
            {
                return !Time.Wait(web3.Eth.Syncing.SendRequestAsync()).IsSyncing;
            }, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(1));


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
