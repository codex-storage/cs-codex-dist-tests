using Logging;
using Nethereum.Contracts;
using NethereumWorkflow;

namespace GethPlugin
{
    public interface IGethNode
    {
        IGethStartResult StartResult { get; }

        Ether GetEthBalance();
        Ether GetEthBalance(IHasEthAddress address);
        Ether GetEthBalance(IEthAddress address);
        void SendEth(IHasEthAddress account, Ether eth);
        void SendEth(IEthAddress account, Ether eth);
        TResult Call<TFunction, TResult>(string contractAddress, TFunction function) where TFunction : FunctionMessage, new();
        void SendTransaction<TFunction>(string contractAddress, TFunction function) where TFunction : FunctionMessage, new();
        decimal? GetSyncedBlockNumber();
        bool IsContractAvailable(string abi, string contractAddress);
    }

    public class GethNode : IGethNode
    {
        private readonly ILog log;

        public GethNode(ILog log, IGethStartResult startResult)
        {
            this.log = log;
            StartResult = startResult;
            Account = startResult.AllAccounts.Accounts.First();
        }

        public IGethStartResult StartResult { get; }
        public GethAccount Account { get; }

        public Ether GetEthBalance()
        {
            return StartInteraction().GetEthBalance().Eth();
        }

        public Ether GetEthBalance(IHasEthAddress owner)
        {
            return GetEthBalance(owner.EthAddress);
        }

        public Ether GetEthBalance(IEthAddress address)
        {
            return StartInteraction().GetEthBalance(address.Address).Eth();
        }

        public void SendEth(IHasEthAddress owner, Ether eth)
        {
            SendEth(owner.EthAddress, eth);
        }

        public void SendEth(IEthAddress account, Ether eth)
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

        private NethereumInteraction StartInteraction()
        {
            var address = StartResult.RunningContainer.Address;
            var account = Account;

            var creator = new NethereumInteractionCreator(log, address.Host, address.Port, account.PrivateKey);
            return creator.CreateWorkflow();
        }

        public decimal? GetSyncedBlockNumber()
        {
            return StartInteraction().GetSyncedBlockNumber();
        }

        public bool IsContractAvailable(string abi, string contractAddress)
        {
            return StartInteraction().IsContractAvailable(abi, contractAddress);
        }
    }
}
