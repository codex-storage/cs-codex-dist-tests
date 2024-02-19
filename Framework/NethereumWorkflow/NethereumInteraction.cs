using Logging;
using Nethereum.ABI.FunctionEncoding.Attributes;
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

        public string SendEth(string toAddress, decimal ethAmount)
        {
            log.Debug();
            var receipt = Time.Wait(web3.Eth.GetEtherTransferService().TransferEtherAndWaitForReceiptAsync(toAddress, ethAmount));
            if (!receipt.Succeeded()) throw new Exception("Unable to send Eth");
            return receipt.TransactionHash;
        }

        public decimal GetEthBalance()
        {
            log.Debug();
            return GetEthBalance(web3.TransactionManager.Account.Address);
        }

        public decimal GetEthBalance(string address)
        {
            log.Debug();
            var balance = Time.Wait(web3.Eth.GetBalance.SendRequestAsync(address));
            return Web3.Convert.FromWei(balance.Value);
        }

        public TResult Call<TFunction, TResult>(string contractAddress, TFunction function) where TFunction : FunctionMessage, new()
        {
            log.Debug(typeof(TFunction).ToString());
            var handler = web3.Eth.GetContractQueryHandler<TFunction>();
            return Time.Wait(handler.QueryAsync<TResult>(contractAddress, function));
        }

        public string SendTransaction<TFunction>(string contractAddress, TFunction function) where TFunction : FunctionMessage, new()
        {
            log.Debug();
            var handler = web3.Eth.GetContractTransactionHandler<TFunction>();
            var receipt = Time.Wait(handler.SendRequestAndWaitForReceiptAsync(contractAddress, function));
            if (!receipt.Succeeded()) throw new Exception("Unable to perform contract transaction.");
            return receipt.TransactionHash;
        }

        public Transaction GetTransaction(string transactionHash)
        {
            log.Debug();
            return Time.Wait(web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(transactionHash));
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

        public List<EventLog<TEvent>> GetEvents<TEvent>(string address, TimeRange timeRange) where TEvent : IEventDTO, new()
        {
            var blockTimeFinder = new BlockTimeFinder(web3, log);

            var lowest = blockTimeFinder.GetLowestBlockNumberAfter(timeRange.From);
            var highest = blockTimeFinder.GetHighestBlockNumberBefore(timeRange.To);

            var fromBlock = Math.Min(lowest, highest);
            var toBlock = Math.Max(lowest, highest);

            return GetEvents<TEvent>(address, fromBlock, toBlock);
        }

        public List<EventLog<TEvent>> GetEvents<TEvent>(string address, ulong fromBlockNumber, ulong toBlockNumber) where TEvent : IEventDTO, new()
        {
            log.Debug($"Getting events of type [{typeof(TEvent).Name}] in block range [{fromBlockNumber} - {toBlockNumber}]");
            var eventHandler = web3.Eth.GetEvent<TEvent>(address);
            var from = new BlockParameter(fromBlockNumber);
            var to = new BlockParameter(toBlockNumber);
            var blockFilter = Time.Wait(eventHandler.CreateFilterBlockRangeAsync(from, to));
            return Time.Wait(eventHandler.GetAllChangesAsync(blockFilter));
        }
    }
}
