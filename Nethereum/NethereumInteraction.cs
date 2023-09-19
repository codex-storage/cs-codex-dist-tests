using Logging;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
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

        public void SendEth(string toAddress, decimal ethAmount)
        {
            var receipt = Time.Wait(web3.Eth.GetEtherTransferService().TransferEtherAndWaitForReceiptAsync(toAddress, ethAmount));
            if (!receipt.Succeeded()) throw new Exception("Unable to send Eth");
        }

        public decimal GetEthBalance()
        {
            return GetEthBalance(web3.TransactionManager.Account.Address);
        }

        public decimal GetEthBalance(string address)
        {
            var balance = Time.Wait(web3.Eth.GetBalance.SendRequestAsync(address));
            return Web3.Convert.FromWei(balance.Value);
        }

        public TResult Call<TFunction, TResult>(string contractAddress, TFunction function) where TFunction : FunctionMessage, new()
        {
            var handler = web3.Eth.GetContractQueryHandler<TFunction>();
            return Time.Wait(handler.QueryAsync<TResult>(contractAddress, function));
        }

        public void SendTransaction<TFunction>(string contractAddress, TFunction function) where TFunction : FunctionMessage, new()
        {
            var handler = web3.Eth.GetContractTransactionHandler<TFunction>();
            var receipt = Time.Wait(handler.SendRequestAndWaitForReceiptAsync(contractAddress, function));
            if (!receipt.Succeeded()) throw new Exception("Unable to perform contract transaction.");
        }

        public decimal? GetSyncedBlockNumber()
        {
            log.Debug();
            var sync = Time.Wait(web3.Eth.Syncing.SendRequestAsync());
            var number = Time.Wait(web3.Eth.Blocks.GetBlockNumber.SendRequestAsync());
            var numberOfBlocks = number.ToDecimal();
            if (sync.IsSyncing) return null;
            return numberOfBlocks;
        }

        public bool IsContractAvailable(string abi, string contractAddress)
        {
            log.Debug();
            try
            {
                var contract = web3.Eth.GetContract(abi, contractAddress);
                return contract != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
