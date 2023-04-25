using Nethereum.Web3;

namespace NethereumWorkflow
{
    public class NethereumInteractionCreator
    {
        private readonly string ip;
        private readonly int port;
        private readonly string rootAccount;
        private readonly string privateKey;

        public NethereumInteractionCreator(string ip, int port, string rootAccount, string privateKey)
        {
            this.ip = ip;
            this.port = port;
            this.rootAccount = rootAccount;
            this.privateKey = privateKey;
        }

        public NethereumInteraction CreateWorkflow()
        {
            return new NethereumInteraction(CreateWeb3(), rootAccount);
        }

        private Web3 CreateWeb3()
        {
            var account = new Nethereum.Web3.Accounts.Account(privateKey);
            return new Web3(account, $"http://{ip}:{port}");
        }
    }
}
