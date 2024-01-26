using GethPlugin;
using NethereumWorkflow;
using Utils;

namespace TestNetRewarder
{
    public interface ICheck
    {
        EthAddress[] Check(ChainState state); 
    }

    public class FilledAnySlotCheck : ICheck
    {
        public EthAddress[] Check(ChainState state)
        {
            return state.SlotFilledEvents.Select(e => e.Host).ToArray();
        }
    }

    public class FinishedSlotCheck : ICheck
    {
        private readonly ByteSize minSize;
        private readonly TimeSpan minDuration;

        public FinishedSlotCheck(ByteSize minSize, TimeSpan minDuration)
        {
            this.minSize = minSize;
            this.minDuration = minDuration;
        }

        public EthAddress[] Check(ChainState state)
        {
            return state.FinishedRequests
                .Where(r =>
                    MeetsSizeRequirement(r) &&
                    MeetsDurationRequirement(r))
                .SelectMany(r => r.Hosts)
                .ToArray();
        }

        private bool MeetsSizeRequirement(StorageRequest r)
        {
            var slotSize = r.Request.Ask.SlotSize.ToDecimal();
            decimal min = minSize.SizeInBytes;
            return slotSize >= min;
        }

        private bool MeetsDurationRequirement(StorageRequest r)
        {
            var duration = TimeSpan.FromSeconds((double)r.Request.Ask.Duration);
            return duration >= minDuration;
        }
    }

    public class PostedContractCheck : ICheck
    {
        public EthAddress[] Check(ChainState state)
        {
            return state.NewRequests.Select(r => r.ClientAddress).ToArray();
        }
    }

    public class StartedContractCheck : ICheck
    {
        private readonly ulong minNumberOfHosts;
        private readonly ByteSize minSlotSize;
        private readonly TimeSpan minDuration;

        public StartedContractCheck(ulong minNumberOfHosts, ByteSize minSlotSize, TimeSpan minDuration)
        {
            this.minNumberOfHosts = minNumberOfHosts;
            this.minSlotSize = minSlotSize;
            this.minDuration = minDuration;
        }

        public EthAddress[] Check(ChainState state)
        {
            return state.StartedRequests
                .Where(r =>
                    MeetsNumSlotsRequirement(r) &&
                    MeetsSizeRequirement(r) &&
                    MeetsDurationRequirement(r))
                .Select(r => r.Request.ClientAddress)
                .ToArray();
        }

        private bool MeetsNumSlotsRequirement(StorageRequest r)
        {
            return r.Request.Ask.Slots >= minNumberOfHosts;
        }

        private bool MeetsSizeRequirement(StorageRequest r)
        {
            var slotSize = r.Request.Ask.SlotSize.ToDecimal();
            decimal min = minSlotSize.SizeInBytes;
            return slotSize >= min;
        }

        private bool MeetsDurationRequirement(StorageRequest r)
        {
            var duration = TimeSpan.FromSeconds((double)r.Request.Ask.Duration);
            return duration >= minDuration;
        }
    }
}
