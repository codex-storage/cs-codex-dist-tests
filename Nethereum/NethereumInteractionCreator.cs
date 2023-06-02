using Logging;
using Nethereum.Web3;

namespace NethereumWorkflow
{
    public class NethereumInteractionCreator
    {
        private readonly BaseLog log;
        private readonly string ip;
        private readonly int port;
        private readonly string privateKey;

        public NethereumInteractionCreator(BaseLog log, string ip, int port, string privateKey)
        {
            this.log = log;
            this.ip = ip;
            this.port = port;
            this.privateKey = privateKey;
        }

        public NethereumInteraction CreateWorkflow()
        {
            return new NethereumInteraction(log, CreateWeb3());
        }

        private Web3 CreateWeb3()
        {
            var account = new Nethereum.Web3.Accounts.Account(privateKey);
            return new Web3(account, $"{ip}:{port}");
        }
    }
}
