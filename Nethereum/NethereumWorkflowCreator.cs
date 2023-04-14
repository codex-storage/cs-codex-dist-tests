using Nethereum.Web3;

namespace NethereumWorkflow
{
    public class NethereumWorkflowCreator
    {
        private readonly string ip;
        private readonly int port;
        private readonly string rootAccount;

        public NethereumWorkflowCreator(string ip, int port, string rootAccount)
        {
            this.ip = ip;
            this.port = port;
            this.rootAccount = rootAccount;
        }

        public NethereumWorkflow CreateWorkflow()
        {
            return new NethereumWorkflow(CreateWeb3(), rootAccount);
        }

        private Web3 CreateWeb3()
        {
            //var bootstrapaccount = new ManagedAccount(bootstrapInfo.Account, "qwerty!@#$%^");
            return new Web3($"http://{ip}:{port}");
        }
    }
}
