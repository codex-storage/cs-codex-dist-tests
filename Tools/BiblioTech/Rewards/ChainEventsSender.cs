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
                    var users = Program.UserRepo.GetAllUserData();

                    foreach (var e in eventsOverview)
                    {
                        if (!string.IsNullOrEmpty(e))
                        {
                            var @event = ApplyReplacements(users, e);
                            await eventsChannel.SendMessageAsync(@event);
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

        private string ApplyReplacements(UserData[] users, string msg)
        {
            var result = ApplyUserAddressReplacements(users, msg);
            return result;
        }

        private string ApplyUserAddressReplacements(UserData[] users, string msg)
        {
            foreach (var user in users)
            {
                if (user.CurrentAddress != null &&
                    !string.IsNullOrEmpty(user.CurrentAddress.Address) &&
                    !string.IsNullOrEmpty(user.Name))
                {
                    msg = msg.Replace(user.CurrentAddress.Address, user.Name);
                }
            }

            return msg;
        }
    }
}
