using DiscordRewards;
using Utils;

namespace TestNetRewarder
{
    public class RequestBuilder
    {
        public EventsAndErrors Build(ChainEventMessage[] lines, string[] errors)
        {
            return new EventsAndErrors
            {
                EventsOverview = lines,
                Errors = errors
            };
        }
    }
}
