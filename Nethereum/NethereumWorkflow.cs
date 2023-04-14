using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using System.Numerics;
using Utils;

namespace NethereumWorkflow
{
    public class NethereumWorkflow
    {
        private readonly Web3 web3;
        private readonly string rootAccount;

        internal NethereumWorkflow(Web3 web3, string rootAccount)
        {
            this.web3 = web3;
            this.rootAccount = rootAccount;
        }

        public void AddToBalance(string account, decimal amount)
        {
            if (amount < 1 || string.IsNullOrEmpty(account)) throw new ArgumentException("Invalid arguments for AddToBalance");

            var value = ToHexBig(amount);
            var transactionId = Time.Wait(web3.Eth.TransactionManager.SendTransactionAsync(rootAccount, account, value));
            Time.Wait(web3.Eth.TransactionManager.TransactionReceiptService.PollForReceiptAsync(transactionId));
        }

        public decimal GetBalance(string account)
        {
            var bigInt = Time.Wait(web3.Eth.GetBalance.SendRequestAsync(account));
            return (decimal)bigInt.Value;
        }

        private HexBigInteger ToHexBig(decimal amount)
        {
            var bigint = new BigInteger(amount);
            var str = bigint.ToString("X");
            return new HexBigInteger(str);
        }
    }
}
