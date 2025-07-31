using BlockchainUtils;
using Core;
using KubernetesWorkflow.Types;
using Logging;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using NethereumWorkflow;
using Utils;

namespace GethPlugin
{
    public interface IGethNode : IHasContainer
    {
        GethDeployment StartResult { get; }
        EthAddress CurrentAddress { get; }

        Ether GetEthBalance();
        Ether GetEthBalance(IHasEthAddress address);
        Ether GetEthBalance(EthAddress address);
        string SendEth(IHasEthAddress account, Ether eth);
        string SendEth(EthAddress account, Ether eth);
        TResult Call<TFunction, TResult>(string contractAddress, TFunction function) where TFunction : FunctionMessage, new();
        TResult Call<TFunction, TResult>(string contractAddress, TFunction function, ulong blockNumber) where TFunction : FunctionMessage, new();
        void Call<TFunction>(string contractAddress, TFunction function) where TFunction : FunctionMessage, new();
        void Call<TFunction>(string contractAddress, TFunction function, ulong blockNumber) where TFunction : FunctionMessage, new();
        string SendTransaction<TFunction>(string contractAddress, TFunction function) where TFunction : FunctionMessage, new();
        Transaction GetTransaction(string transactionHash);
        decimal? GetSyncedBlockNumber();
        bool IsContractAvailable(string abi, string contractAddress);
        GethBootstrapNode GetBootstrapRecord();
        List<EventLog<TEvent>> GetEvents<TEvent>(string address, BlockInterval blockRange) where TEvent : IEventDTO, new();
        List<EventLog<TEvent>> GetEvents<TEvent>(string address, TimeRange timeRange) where TEvent : IEventDTO, new();
        BlockInterval ConvertTimeRangeToBlockRange(TimeRange timeRange);
        BlockTimeEntry GetBlockForNumber(ulong number);
        void IterateFunctionCalls<TFunc>(BlockInterval blockInterval, Action<BlockTimeEntry, TFunc> onCall) where TFunc : FunctionMessage, new();
        IGethNode WithDifferentAccount(EthAccount account);
    }

    public class DeploymentGethNode : BaseGethNode, IGethNode
    {
        private readonly ILog log;
        private readonly BlockCache blockCache;

        public DeploymentGethNode(ILog log, BlockCache blockCache, GethDeployment startResult)
        {
            this.log = log;
            this.blockCache = blockCache;
            StartResult = startResult;
            CurrentAddress = new EthAddress(startResult.Account.Account);
        }

        public GethDeployment StartResult { get; }
        public RunningContainer Container => StartResult.Container;
        public EthAddress CurrentAddress { get; }

        public GethBootstrapNode GetBootstrapRecord()
        {
            var address = StartResult.Container.GetInternalAddress(GethContainerRecipe.ListenPortTag);

            return new GethBootstrapNode(
                publicKey: StartResult.PubKey,
                ipAddress: address.Host.Replace("http://", ""),
                port: address.Port
            );
        }

        protected override NethereumInteraction StartInteraction()
        {
            var address = StartResult.Container.GetAddress(GethContainerRecipe.HttpPortTag);
            var account = StartResult.Account;

            var creator = new NethereumInteractionCreator(log, blockCache, address.Host, address.Port, account.PrivateKey);
            return creator.CreateWorkflow();
        }

        public IGethNode WithDifferentAccount(EthAccount account)
        {
            return new DeploymentGethNode(log, blockCache,
                new GethDeployment(
                    StartResult.Pod,
                    StartResult.DiscoveryPort,
                    StartResult.HttpPort,
                    StartResult.WsPort,
                    new GethAccount(
                        account.EthAddress.Address,
                        account.PrivateKey
                    ),
                    account.PrivateKey));
        }
    }

    public class CustomGethNode : BaseGethNode, IGethNode
    {
        private readonly ILog log;
        private readonly BlockCache blockCache;
        private readonly string gethHost;
        private readonly int gethPort;
        private readonly string privateKey;

        public GethDeployment StartResult => throw new NotImplementedException();
        public RunningContainer Container => throw new NotImplementedException();
        public EthAddress CurrentAddress { get; }

        public CustomGethNode(ILog log, BlockCache blockCache, string gethHost, int gethPort, string privateKey)
        {
            this.log = log;
            this.blockCache = blockCache;
            this.gethHost = gethHost;
            this.gethPort = gethPort;
            this.privateKey = privateKey;

            var creator = new NethereumInteractionCreator(log, blockCache, gethHost, gethPort, privateKey);
            CurrentAddress = creator.GetEthAddress();
        }

        public GethBootstrapNode GetBootstrapRecord()
        {
            throw new NotImplementedException();
        }

        public IGethNode WithDifferentAccount(EthAccount account)
        {
            return new CustomGethNode(log, blockCache, gethHost, gethPort, account.PrivateKey);
        }

        protected override NethereumInteraction StartInteraction()
        {
            var creator = new NethereumInteractionCreator(log, blockCache, gethHost, gethPort, privateKey);
            return creator.CreateWorkflow();
        }
    }

    public abstract class BaseGethNode
    {
        public Ether GetEthBalance()
        {
            return StartInteraction().GetEthBalance().Eth();
        }

        public Ether GetEthBalance(IHasEthAddress owner)
        {
            return GetEthBalance(owner.EthAddress);
        }

        public Ether GetEthBalance(EthAddress address)
        {
            return StartInteraction().GetEthBalance(address.Address).Wei();
        }

        public string SendEth(IHasEthAddress owner, Ether eth)
        {
            return SendEth(owner.EthAddress, eth);
        }

        public string SendEth(EthAddress account, Ether eth)
        {
            return StartInteraction().SendEth(account.Address, eth.Eth);
        }

        public TResult Call<TFunction, TResult>(string contractAddress, TFunction function) where TFunction : FunctionMessage, new()
        {
            return StartInteraction().Call<TFunction, TResult>(contractAddress, function);
        }

        public TResult Call<TFunction, TResult>(string contractAddress, TFunction function, ulong blockNumber) where TFunction : FunctionMessage, new()
        {
            return StartInteraction().Call<TFunction, TResult>(contractAddress, function, blockNumber);
        }

        public void Call<TFunction>(string contractAddress, TFunction function) where TFunction : FunctionMessage, new()
        {
            StartInteraction().Call(contractAddress, function);
        }

        public void Call<TFunction>(string contractAddress, TFunction function, ulong blockNumber) where TFunction : FunctionMessage, new()
        {
            StartInteraction().Call(contractAddress, function, blockNumber);
        }

        public string SendTransaction<TFunction>(string contractAddress, TFunction function) where TFunction : FunctionMessage, new()
        {
            return StartInteraction().SendTransaction(contractAddress, function);
        }

        public Transaction GetTransaction(string transactionHash)
        {
            return StartInteraction().GetTransaction(transactionHash);
        }

        public decimal? GetSyncedBlockNumber()
        {
            return StartInteraction().GetSyncedBlockNumber();
        }

        public bool IsContractAvailable(string abi, string contractAddress)
        {
            return StartInteraction().IsContractAvailable(abi, contractAddress);
        }

        public List<EventLog<TEvent>> GetEvents<TEvent>(string address, BlockInterval blockRange) where TEvent : IEventDTO, new()
        {
            return StartInteraction().GetEvents<TEvent>(address, blockRange);
        }

        public List<EventLog<TEvent>> GetEvents<TEvent>(string address, TimeRange timeRange) where TEvent : IEventDTO, new()
        {
            return StartInteraction().GetEvents<TEvent>(address, ConvertTimeRangeToBlockRange(timeRange));
        }

        public BlockInterval ConvertTimeRangeToBlockRange(TimeRange timeRange)
        {
            return StartInteraction().ConvertTimeRangeToBlockRange(timeRange);
        }

        public BlockTimeEntry GetBlockForNumber(ulong number)
        {
            return StartInteraction().GetBlockForNumber(number);
        }

        public BlockWithTransactions GetBlk(ulong number)
        {
            return StartInteraction().GetBlockWithTransactions(number);
        }

        public void IterateFunctionCalls<TFunc>(BlockInterval blockRange, Action<BlockTimeEntry, TFunc> onCall) where TFunc : FunctionMessage, new()
        {
            var i = StartInteraction();
            for (var blkI = blockRange.From; blkI <= blockRange.To; blkI++)
            {
                var blk = i.GetBlockWithTransactions(blkI);

                foreach (var t in blk.Transactions)
                {
                    if (t.IsTransactionForFunctionMessage<TFunc>())
                    {
                        var func = t.DecodeTransactionToFunctionMessage<TFunc>();
                        if (func != null)
                        {
                            var b = GetBlockForNumber(blkI);
                            onCall(b, func);
                        }
                    }
                }
            }
        }

        protected abstract NethereumInteraction StartInteraction();
    }
}
