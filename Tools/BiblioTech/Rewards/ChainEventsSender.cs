using Discord.WebSocket;
using Logging;

namespace BiblioTech.Rewards
{
    public class ChainEventsSender
    {
        private readonly ILog log;
        private SocketTextChannel? eventsChannel;

        public ChainEventsSender(ILog log, SocketTextChannel? eventsChannel)
        {
            this.log = log;
            this.eventsChannel = eventsChannel;
        }

        public async Task ProcessChainEvents(string[] eventsOverview)
        {
            if (eventsChannel == null || eventsOverview == null || !eventsOverview.Any()) return;
            try
            {
                await Task.Run(async () =>
                {
                    foreach (var e in eventsOverview)
                    {
                        if (!string.IsNullOrEmpty(e))
                        {
                            await eventsChannel.SendMessageAsync(e);
                            await Task.Delay(3000);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                log.Error("Failed to process chain events: " + ex);
            }
        }
    }
}
