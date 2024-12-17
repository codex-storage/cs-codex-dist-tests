using BlockchainUtils;
using Logging;
using Nethereum.Web3;

namespace NethereumWorkflow
{
    public class NethereumInteractionCreator
    {
        private readonly ILog log;
        private readonly BlockCache blockCache;
        private readonly string ip;
        private readonly int port;
        private readonly string privateKey;

        public NethereumInteractionCreator(ILog log, BlockCache blockCache, string ip, int port, string privateKey)
        {
            this.log = log;
            this.blockCache = blockCache;
            this.ip = ip;
            this.port = port;
            this.privateKey = privateKey;
        }

        public NethereumInteraction CreateWorkflow()
        {
            log.Debug("Starting interaction to " + ip + ":" + port);
            return new NethereumInteraction(log, CreateWeb3(), blockCache);
        }

        private Web3 CreateWeb3()
        {
            var account = new Nethereum.Web3.Accounts.Account(privateKey);
            return new Web3(account, $"{ip}:{port}");
        }
    }
}
