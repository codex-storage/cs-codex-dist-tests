using Utils;

namespace DiscordRewards
{
    public class CheckConfig
    {
        public CheckType Type { get; set; }
        public ulong MinNumberOfHosts { get; set; }
        public ByteSize MinSlotSize { get; set; } = 0.Bytes();
        public TimeSpan MinDuration { get; set; } = TimeSpan.Zero;
    }

    public enum CheckType
    {
        Uninitialized,
        HostFilledSlot,
        HostFinishedSlot,
        ClientPostedContract,
        ClientStartedContract,
    }
}
