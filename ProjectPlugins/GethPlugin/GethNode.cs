using Core;
using KubernetesWorkflow.Types;
using Logging;
using Nethereum.Contracts;
using NethereumWorkflow;

namespace GethPlugin
{
    public interface IGethNode : IHasContainer
    {
        GethDeployment StartResult { get; }

        Ether GetEthBalance();
        Ether GetEthBalance(IHasEthAddress address);
        Ether GetEthBalance(EthAddress address);
        void SendEth(IHasEthAddress account, Ether eth);
        void SendEth(EthAddress account, Ether eth);
        TResult Call<TFunction, TResult>(string contractAddress, TFunction function) where TFunction : FunctionMessage, new();
        void SendTransaction<TFunction>(string contractAddress, TFunction function) where TFunction : FunctionMessage, new();
        decimal? GetSyncedBlockNumber();
        bool IsContractAvailable(string abi, string contractAddress);
        GethBootstrapNode GetBootstrapRecord();
    }

    public class DeploymentGethNode : BaseGethNode, IGethNode
    {
        private readonly ILog log;

        public DeploymentGethNode(ILog log, GethDeployment startResult)
        {
            this.log = log;
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
            var address = StartResult.Container.GetAddress(log, GethContainerRecipe.HttpPortTag);
            var account = StartResult.Account;

            var creator = new NethereumInteractionCreator(log, address.Host, address.Port, account.PrivateKey);
            return creator.CreateWorkflow();
        }
    }

    public class CustomGethNode : BaseGethNode, IGethNode
    {
        private readonly ILog log;
        private readonly string gethHost;
        private readonly int gethPort;
        private readonly string privateKey;

        public GethDeployment StartResult => throw new NotImplementedException();
        public RunningContainer Container => throw new NotImplementedException();

        public CustomGethNode(ILog log, string gethHost, int gethPort, string privateKey)
        {
            this.log = log;
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
            var creator = new NethereumInteractionCreator(log, gethHost, gethPort, privateKey);
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

        public void SendEth(IHasEthAddress owner, Ether eth)
        {
            SendEth(owner.EthAddress, eth);
        }

        public void SendEth(EthAddress account, Ether eth)
        {
            StartInteraction().SendEth(account.Address, eth.Eth);
        }

        public TResult Call<TFunction, TResult>(string contractAddress, TFunction function) where TFunction : FunctionMessage, new()
        {
            return StartInteraction().Call<TFunction, TResult>(contractAddress, function);
        }

        public void SendTransaction<TFunction>(string contractAddress, TFunction function) where TFunction : FunctionMessage, new()
        {
            StartInteraction().SendTransaction(contractAddress, function);
        }

        public decimal? GetSyncedBlockNumber()
        {
            return StartInteraction().GetSyncedBlockNumber();
        }

        public bool IsContractAvailable(string abi, string contractAddress)
        {
            return StartInteraction().IsContractAvailable(abi, contractAddress);
        }

        protected abstract NethereumInteraction StartInteraction();
    }
}
