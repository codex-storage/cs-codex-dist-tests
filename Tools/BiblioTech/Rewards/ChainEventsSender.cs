using Discord.WebSocket;
using DiscordRewards;
using Logging;

namespace BiblioTech.Rewards
{
    public class ChainEventsSender
    {
        private readonly ILog log;
        private readonly CustomReplacement replacement;
        private readonly SocketTextChannel? eventsChannel;

        public ChainEventsSender(ILog log, CustomReplacement replacement, SocketTextChannel? eventsChannel)
        {
            this.log = log;
            this.replacement = replacement;
            this.eventsChannel = eventsChannel;
        }

        public async Task ProcessChainEvents(ChainEventMessage[] eventsOverview, string[] errors)
        {
            await SendErrorsToAdminChannel(errors);

            if (eventsChannel == null || eventsOverview == null || !eventsOverview.Any()) return;
            try
            {
                await Task.Run(async () =>
                {
                    var users = Program.UserRepo.GetAllUserData();
                    await SendChainEventsInOrder(eventsOverview, eventsChannel, users);
                });
            }
            catch (Exception ex)
            {
                log.Error("Failed to process chain events: " + ex);
            }
        }

        private async Task SendErrorsToAdminChannel(string[] errors)
        {
            try
            {
                foreach (var error in errors)
                {
                    await Program.AdminChecker.SendInAdminChannel(error);
                }
            }
            catch (Exception exc)
            {
                log.Error("Failed to send error messages to admin channel. " + exc);
                Environment.Exit(1);
            }
        }

        private async Task SendChainEventsInOrder(ChainEventMessage[] eventsOverview, SocketTextChannel eventsChannel, UserData[] users)
        {
            eventsOverview = eventsOverview.OrderBy(e => e.BlockNumber).ToArray();
            foreach (var e in eventsOverview)
            {
                var msg = e.Message;
                if (!string.IsNullOrEmpty(msg))
                {
                    var @event = ApplyReplacements(users, msg);
                    await eventsChannel.SendMessageAsync(@event);
                    await Task.Delay(300);
                }
            }
        }

        private string ApplyReplacements(UserData[] users, string msg)
        {
            var result = ApplyUserAddressReplacements(users, msg);
            result = ApplyCustomReplacements(result);
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

        private string ApplyCustomReplacements(string result)
        {
            return replacement.Apply(result);
        }
    }
}
