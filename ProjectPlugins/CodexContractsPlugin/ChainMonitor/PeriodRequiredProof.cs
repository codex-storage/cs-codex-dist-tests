using Nethereum.Hex.HexConvertors.Extensions;
using Utils;

namespace CodexContractsPlugin.ChainMonitor
{
    public class PeriodRequiredProof
    {
        public PeriodRequiredProof(EthAddress host, IChainStateRequest request, int slotIndex)
        {
            Host = host;
            Request = request;
            SlotIndex = slotIndex;
        }

        public EthAddress Host { get; }
        public IChainStateRequest Request { get; }
        public int SlotIndex { get; }

        public string Describe()
        {
            return $"{Request.RequestId.ToHex()} slotIndex:{SlotIndex} by {Host}";
        }
    }
}
