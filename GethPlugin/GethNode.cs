using Logging;
using NethereumWorkflow;

namespace GethPlugin
{
    public interface IGethNode
    {
        IGethStartResult StartResult { get; }

        NethereumInteraction StartInteraction();
        Ether GetEthBalance();
        Ether GetEthBalance(IHasEthAddress address);
        Ether GetEthBalance(IEthAddress address);
        void SendEth(IHasEthAddress account, Ether eth);
        void SendEth(IEthAddress account, Ether eth);
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

        public NethereumInteraction StartInteraction()
        {
            var address = StartResult.RunningContainer.Address;
            var account = Account;

            var creator = new NethereumInteractionCreator(log, address.Host, address.Port, account.PrivateKey);
            return creator.CreateWorkflow();
        }

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
            var i = StartInteraction();
            i.SendEth(account.Address, eth.Eth);
        }
    }
}
