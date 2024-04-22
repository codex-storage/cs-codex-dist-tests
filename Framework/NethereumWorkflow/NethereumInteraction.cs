using Logging;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using NethereumWorkflow.BlockUtils;
using Utils;

namespace NethereumWorkflow
{
    public class NethereumInteraction
    {
        // BlockCache is a static instance: It stays alive for the duration of the application runtime.
        private readonly static BlockCache blockCache = new BlockCache();

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

        public List<EventLog<TEvent>> GetEvents<TEvent>(string address, BlockInterval blockRange) where TEvent : IEventDTO, new()
        {
            return GetEvents<TEvent>(address, blockRange.From, blockRange.To);
        }

        public List<EventLog<TEvent>> GetEvents<TEvent>(string address, ulong fromBlockNumber, ulong toBlockNumber) where TEvent : IEventDTO, new()
        {
            var eventHandler = web3.Eth.GetEvent<TEvent>(address);
            var from = new BlockParameter(fromBlockNumber);
            var to = new BlockParameter(toBlockNumber);
            var blockFilter = Time.Wait(eventHandler.CreateFilterBlockRangeAsync(from, to));
            return Time.Wait(eventHandler.GetAllChangesAsync(blockFilter));
        }

        public BlockInterval ConvertTimeRangeToBlockRange(TimeRange timeRange)
        {
            var wrapper = new Web3Wrapper(web3, log);
            var blockTimeFinder = new BlockTimeFinder(blockCache, wrapper, log);

            var fromBlock = blockTimeFinder.GetLowestBlockNumberAfter(timeRange.From);
            var toBlock = blockTimeFinder.GetHighestBlockNumberBefore(timeRange.To);

            if (fromBlock == null  || toBlock == null)
            {
                throw new Exception("Failed to convert time range to block range.");
            }

            return new BlockInterval(
                from: fromBlock.Value,
                to: toBlock.Value
            );
        }

        public BlockTimeEntry GetBlockForNumber(ulong number)
        {
            var wrapper = new Web3Wrapper(web3, log);
            var blockTimeFinder = new BlockTimeFinder(blockCache, wrapper, log);
            return blockTimeFinder.Get(number);
        }
    }
}
