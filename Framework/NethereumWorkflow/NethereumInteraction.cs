using System.Numerics;
using BlockchainUtils;
using Logging;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Model;
using Nethereum.RPC.Eth.Blocks;
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
            return DebugLogWrap(() =>
            {
                var receipt = Time.Wait(web3.Eth.GetEtherTransferService().TransferEtherAndWaitForReceiptAsync(toAddress, ((decimal)ethAmount)));
                if (!receipt.Succeeded()) throw new Exception("Unable to send Eth");
                return receipt.TransactionHash;
            }, nameof(SendEth));
        }

        public BigInteger GetEthBalance()
        {
            return DebugLogWrap(() =>
            {
                return GetEthBalance(web3.TransactionManager.Account.Address);
            }, nameof(GetEthBalance));
        }

        public BigInteger GetEthBalance(string address)
        {
            return DebugLogWrap(() =>
            {
                var balance = Time.Wait(web3.Eth.GetBalance.SendRequestAsync(address));
                return balance.Value;
            }, nameof(GetEthBalance));
        }

        public TResult Call<TFunction, TResult>(string contractAddress, TFunction function) where TFunction : FunctionMessage, new()
        {
            return DebugLogWrap(() =>
            {
                var handler = web3.Eth.GetContractQueryHandler<TFunction>();
                return Time.Wait(handler.QueryAsync<TResult>(contractAddress, function));
            }, nameof(Call) + "." + typeof(TFunction).ToString());
        }

        public TResult Call<TFunction, TResult>(string contractAddress, TFunction function, ulong blockNumber) where TFunction : FunctionMessage, new()
        {
            return DebugLogWrap(() =>
            {
                var handler = web3.Eth.GetContractQueryHandler<TFunction>();
                return Time.Wait(handler.QueryAsync<TResult>(contractAddress, function, new BlockParameter(blockNumber)));
            }, nameof(Call) + "." + typeof(TFunction).ToString());
        }

        public void Call<TFunction>(string contractAddress, TFunction function) where TFunction : FunctionMessage, new()
        {
            DebugLogWrap<string>(() =>
            {
                var handler = web3.Eth.GetContractQueryHandler<TFunction>();
                Time.Wait(handler.QueryRawAsync(contractAddress, function));
                return string.Empty;
            }, nameof(Call) + "." + typeof(TFunction).ToString());
        }

        public void Call<TFunction>(string contractAddress, TFunction function, ulong blockNumber) where TFunction : FunctionMessage, new()
        {
            DebugLogWrap<string>(() =>
            {
                var handler = web3.Eth.GetContractQueryHandler<TFunction>();
                var result = Time.Wait(handler.QueryRawAsync(contractAddress, function, new BlockParameter(blockNumber)));
                return string.Empty;
            }, nameof(Call) + "." + typeof(TFunction).ToString());
        }

        public string SendTransaction<TFunction>(string contractAddress, TFunction function) where TFunction : FunctionMessage, new()
        {
            return DebugLogWrap(() =>
            {
                var handler = web3.Eth.GetContractTransactionHandler<TFunction>();
                var receipt = Time.Wait(handler.SendRequestAndWaitForReceiptAsync(contractAddress, function));
                if (!receipt.Succeeded()) throw new Exception("Unable to perform contract transaction.");
                return receipt.TransactionHash;
            }, nameof(SendTransaction) + "." + typeof(TFunction).ToString());
        }

        public Transaction GetTransaction(string transactionHash)
        {
            return DebugLogWrap(() =>
            {
                return Time.Wait(web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(transactionHash));
            }, nameof(GetTransaction));
        }

        public decimal? GetSyncedBlockNumber()
        {
            return DebugLogWrap<decimal?>(() =>
            {
                var sync = Time.Wait(web3.Eth.Syncing.SendRequestAsync());
                var number = Time.Wait(web3.Eth.Blocks.GetBlockNumber.SendRequestAsync());
                var numberOfBlocks = number.ToDecimal();
                if (sync.IsSyncing) return null;
                return numberOfBlocks;
            }, nameof(GetTransaction));
        }

        public bool IsContractAvailable(string abi, string contractAddress)
        {
            return DebugLogWrap(() =>
            {
                try
                {
                    var contract = web3.Eth.GetContract(abi, contractAddress);
                    return contract != null;
                }
                catch
                {
                    return false;
                }
            }, nameof(IsContractAvailable));
        }

        public List<EventLog<TEvent>> GetEvents<TEvent>(string address, BlockInterval blockRange) where TEvent : IEventDTO, new()
        {
            return DebugLogWrap(() =>
            {
                return GetEvents<TEvent>(address, blockRange.From, blockRange.To);
            }, nameof(GetEvents) + "." + typeof(TEvent).ToString());
        }

        public List<EventLog<TEvent>> GetEvents<TEvent>(string address, ulong fromBlockNumber, ulong toBlockNumber) where TEvent : IEventDTO, new()
        {
            return DebugLogWrap(() =>
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
            }, nameof(GetEvents) + "." + typeof(TEvent).ToString());
        }

        public BlockInterval ConvertTimeRangeToBlockRange(TimeRange timeRange)
        {
            return DebugLogWrap(() =>
            {
                if (timeRange.To - timeRange.From < TimeSpan.FromSeconds(1.0))
                    throw new Exception(nameof(ConvertTimeRangeToBlockRange) + ": Time range too small.");

                var wrapper = new Web3Wrapper(web3, log);
                var blockTimeFinder = new BlockTimeFinder(blockCache, wrapper, log);

                var fromBlock = blockTimeFinder.GetLowestBlockNumberAfter(timeRange.From);
                var toBlock = blockTimeFinder.GetHighestBlockNumberBefore(timeRange.To);

                if (fromBlock == null || toBlock == null)
                {
                    throw new Exception("Failed to convert time range to block range.");
                }

                return new BlockInterval(
                    timeRange: timeRange,
                    from: fromBlock.Value,
                    to: toBlock.Value
                );
            }, nameof(ConvertTimeRangeToBlockRange));
        }

        public BlockTimeEntry GetBlockForNumber(ulong number)
        {
            return DebugLogWrap(() =>
            {
                var wrapper = new Web3Wrapper(web3, log);
                var blockTimeFinder = new BlockTimeFinder(blockCache, wrapper, log);
                return blockTimeFinder.Get(number);
            }, nameof(GetBlockForNumber));
        }

        public BlockWithTransactions GetBlockWithTransactions(ulong number)
        {
            return DebugLogWrap(() =>
            {
                var retry = new Retry(nameof(GetBlockWithTransactions),
                    maxTimeout: TimeSpan.FromMinutes(1.0),
                    sleepAfterFail: TimeSpan.FromSeconds(1.0),
                    onFail: f => { },
                    failFast: false);

                return retry.Run(() => Time.Wait(web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new BlockParameter(number))));
            }, nameof(GetBlockWithTransactions));
        }

        private T DebugLogWrap<T>(Func<T> task, string name = "")
        {
            log.Debug($"{name} start...", 1);
            var result = task();
            log.Debug($"{name} finished", 1);
            return result;
        }
    }
}
