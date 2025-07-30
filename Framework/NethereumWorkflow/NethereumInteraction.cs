using System.Numerics;
using BlockchainUtils;
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
        private readonly BlockCache blockCache;

        private readonly ILog log;
        private readonly Web3 web3;

        internal NethereumInteraction(ILog log, Web3 web3, BlockCache blockCache)
        {
            this.log = log;
            this.web3 = web3;
            this.blockCache = blockCache;
        }


        public string SendEth(string toAddress, BigInteger ethAmount)
        {
            log.Debug();
            var receipt = Time.Wait(web3.Eth.GetEtherTransferService().TransferEtherAndWaitForReceiptAsync(toAddress, ((decimal)ethAmount)));
            if (!receipt.Succeeded()) throw new Exception("Unable to send Eth");
            return receipt.TransactionHash;
        }

        public BigInteger GetEthBalance()
        {
            log.Debug();
            return GetEthBalance(web3.TransactionManager.Account.Address);
        }

        public BigInteger GetEthBalance(string address)
        {
            log.Debug();
            var balance = Time.Wait(web3.Eth.GetBalance.SendRequestAsync(address));
            return balance.Value;
        }

        public TResult Call<TFunction, TResult>(string contractAddress, TFunction function) where TFunction : FunctionMessage, new()
        {
            log.Debug(typeof(TFunction).ToString());
            var handler = web3.Eth.GetContractQueryHandler<TFunction>();
            return Time.Wait(handler.QueryAsync<TResult>(contractAddress, function));
        }

        public TResult Call<TFunction, TResult>(string contractAddress, TFunction function, ulong blockNumber) where TFunction : FunctionMessage, new()
        {
            log.Debug(typeof(TFunction).ToString());
            var handler = web3.Eth.GetContractQueryHandler<TFunction>();
            return Time.Wait(handler.QueryAsync<TResult>(contractAddress, function, new BlockParameter(blockNumber)));
        }

        public void Call<TFunction>(string contractAddress, TFunction function) where TFunction : FunctionMessage, new()
        {
            log.Debug(typeof(TFunction).ToString());
            var handler = web3.Eth.GetContractQueryHandler<TFunction>();
            Time.Wait(handler.QueryRawAsync(contractAddress, function));
        }

        public void Call<TFunction>(string contractAddress, TFunction function, ulong blockNumber) where TFunction : FunctionMessage, new()
        {
            log.Debug(typeof(TFunction).ToString());
            var handler = web3.Eth.GetContractQueryHandler<TFunction>();
            var result = Time.Wait(handler.QueryRawAsync(contractAddress, function, new BlockParameter(blockNumber)));
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
            var logs = new List<FilterLog>();
            var p = web3.Processing.Logs.CreateProcessor(
                action: logs.Add,
                minimumBlockConfirmations: 1,
                criteria: l => l.IsLogForEvent<TEvent>()
            );

            var from = new BlockParameter(fromBlockNumber);
            var to = new BlockParameter(toBlockNumber);
            var ct = new CancellationTokenSource().Token;
            Time.Wait(p.ExecuteAsync(toBlockNumber: to.BlockNumber, cancellationToken: ct, startAtBlockNumberIfNotProcessed: from.BlockNumber));

            return logs
                .Where(l => l.IsLogForEvent<TEvent>())
                .Select(l => l.DecodeEvent<TEvent>())
                .ToList();
        }

        public BlockInterval ConvertTimeRangeToBlockRange(TimeRange timeRange)
        {
            if (timeRange.To - timeRange.From < TimeSpan.FromSeconds(1.0))
                throw new Exception(nameof(ConvertTimeRangeToBlockRange) + ": Time range too small.");

            var wrapper = new Web3Wrapper(web3, log);
            var blockTimeFinder = new BlockTimeFinder(blockCache, wrapper, log);

            var fromBlock = blockTimeFinder.GetLowestBlockNumberAfter(timeRange.From);
            var toBlock = blockTimeFinder.GetHighestBlockNumberBefore(timeRange.To);

            if (fromBlock == null  || toBlock == null)
            {
                throw new Exception("Failed to convert time range to block range.");
            }

            return new BlockInterval(
                timeRange: timeRange,
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

        public BlockWithTransactions GetBlockWithTransactions(ulong number)
        {
            var retry = new Retry(nameof(GetBlockWithTransactions),
                maxTimeout: TimeSpan.FromMinutes(1.0),
                sleepAfterFail: TimeSpan.FromSeconds(1.0),
                onFail: f => { },
                failFast: false);

            return retry.Run(() => Time.Wait(web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new BlockParameter(number))));
        }
    }
}
