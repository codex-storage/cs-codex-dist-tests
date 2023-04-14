using Logging;
using Nethereum.Web3;

namespace NethereumWorkflow
{
    public class NethereumInteractionCreator
    {
        private readonly TestLog log;
        private readonly string ip;
        private readonly int port;
        private readonly string rootAccount;

        public NethereumInteractionCreator(TestLog log, string ip, int port, string rootAccount)
        {
            this.log = log;
            this.ip = ip;
            this.port = port;
            this.rootAccount = rootAccount;
        }

        public NethereumInteraction CreateWorkflow()
        {
            return new NethereumInteraction(log, CreateWeb3(), rootAccount);
        }

        private Web3 CreateWeb3()
        {
            //var bootstrapaccount = new ManagedAccount(bootstrapInfo.Account, "qwerty!@#$%^");
            return new Web3($"http://{ip}:{port}");
        }
    }
}
