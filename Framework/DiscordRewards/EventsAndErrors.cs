namespace DiscordRewards
{
    public class EventsAndErrors
    {
        public ChainEventMessage[] EventsOverview { get; set; } = Array.Empty<ChainEventMessage>();
        public string[] Errors { get; set; } = Array.Empty<string>();
        public ActiveChainAddresses ActiveChainAddresses { get; set; } = new ActiveChainAddresses();

        public bool HasAny()
        {
            return
                Errors.Length > 0 ||
                EventsOverview.Length > 0 ||
                ActiveChainAddresses.HasAny();
        }
    }

    public class ChainEventMessage
    {
        public ulong BlockNumber { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ActiveChainAddresses
    {
        public string[] Hosts { get; set; } = Array.Empty<string>();
        public string[] Clients { get; set; } = Array.Empty<string>();

        public bool HasAny()
        {
            return Hosts.Length > 0 || Clients.Length > 0;
        }
    }
}
