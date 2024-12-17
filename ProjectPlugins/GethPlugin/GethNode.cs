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

        Ether GetEthBalance();
        Ether GetEthBalance(IHasEthAddress address);
        Ether GetEthBalance(EthAddress address);
        string SendEth(IHasEthAddress account, Ether eth);
        string SendEth(EthAddress account, Ether eth);
        TResult Call<TFunction, TResult>(string contractAddress, TFunction function) where TFunction : FunctionMessage, new();
        string SendTransaction<TFunction>(string contractAddress, TFunction function) where TFunction : FunctionMessage, new();
        Transaction GetTransaction(string transactionHash);
        decimal? GetSyncedBlockNumber();
        bool IsContractAvailable(string abi, string contractAddress);
        GethBootstrapNode GetBootstrapRecord();
        List<EventLog<TEvent>> GetEvents<TEvent>(string address, BlockInterval blockRange) where TEvent : IEventDTO, new();
        List<EventLog<TEvent>> GetEvents<TEvent>(string address, TimeRange timeRange) where TEvent : IEventDTO, new();
        BlockInterval ConvertTimeRangeToBlockRange(TimeRange timeRange);
        BlockTimeEntry GetBlockForNumber(ulong number);
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
        }

        public GethDeployment StartResult { get; }
        public RunningContainer Container => StartResult.Container;

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

        public CustomGethNode(ILog log, BlockCache blockCache, string gethHost, int gethPort, string privateKey)
        {
            this.log = log;
            this.blockCache = blockCache;
            this.gethHost = gethHost;
            this.gethPort = gethPort;
            this.privateKey = privateKey;
        }

        public GethBootstrapNode GetBootstrapRecord()
        {
            throw new NotImplementedException();
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
            return StartInteraction().GetEthBalance(address.Address).Eth();
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

        protected abstract NethereumInteraction StartInteraction();
    }
}
