using BlockchainUtils;
using Logging;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Utils;

namespace NethereumWorkflow
{
    public class Web3Wrapper : IWeb3Blocks
    {
        private readonly Web3 web3;
        private readonly ILog log;

        public Web3Wrapper(Web3 web3, ILog log)
        {
            this.web3 = web3;
            this.log = log;
        }

        public ulong GetCurrentBlockNumber()
        {
            var number = Time.Wait(web3.Eth.Blocks.GetBlockNumber.SendRequestAsync());
            return Convert.ToUInt64(number.ToDecimal());
        }

        public DateTime? GetTimestampForBlock(ulong blockNumber)
        {
            try
            {
                var block = Time.Wait(web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new BlockParameter(blockNumber)));
                if (block == null) return null;
                return DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(block.Timestamp.ToDecimal())).UtcDateTime;
            }
            catch (Exception ex)
            {
                log.Error("Exception while getting timestamp for block: " + ex);
                return null;
            }
        }
    }
}
