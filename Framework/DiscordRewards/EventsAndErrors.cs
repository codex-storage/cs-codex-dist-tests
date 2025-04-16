namespace DiscordRewards
{
    public class EventsAndErrors
    {
        public ChainEventMessage[] EventsOverview { get; set; } = Array.Empty<ChainEventMessage>();
        public string[] Errors { get; set; } = Array.Empty<string>();

        public bool HasAny()
        {
            return Errors.Length > 0 || EventsOverview.Length > 0;
        }
    }

    public class ChainEventMessage
    {
        public ulong BlockNumber { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
