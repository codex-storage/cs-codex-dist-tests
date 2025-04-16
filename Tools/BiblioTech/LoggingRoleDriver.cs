using BiblioTech.Rewards;
using Discord;
using DiscordRewards;
using Logging;
using Newtonsoft.Json;

namespace BiblioTech
{
    public class LoggingRoleDriver : IDiscordRoleDriver
    {
        private readonly ILog log;

        public LoggingRoleDriver(ILog log)
        {
            this.log = log;
        }

        public async Task RunRoleGiver(Func<IRoleGiver, Task> action)
        {
            await Task.CompletedTask;
            await action(new LoggingRoleGiver(log));
        }

        public async Task IterateRemoveActiveP2pParticipants(Func<IUser, bool> predicate)
        {
            await Task.CompletedTask;
        }

        private class LoggingRoleGiver : IRoleGiver
        {
            private readonly ILog log;

            public LoggingRoleGiver(ILog log)
            {
                this.log = log;
            }

            public async Task GiveActiveP2pParticipant(IUser user)
            {
                log.Log($"Giving ActiveP2p role to " + user.Id);
                await Task.CompletedTask;
            }

            public async Task GiveAltruisticRole(IUser user)
            {
                log.Log($"Giving Altruistic role to " + user.Id);
                await Task.CompletedTask;
            }
        }
    }
}
