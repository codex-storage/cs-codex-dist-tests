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

        public async Task IterateUsersWithRoles(Func<IRoleGiver, IUser, ulong, Task> onUserWithRole, params ulong[] rolesToIterate)
        {
            await Task.CompletedTask;
        }

        public async Task IterateUsersWithRoles(Func<IRoleGiver, IUser, ulong, Task> onUserWithRole, Func<IRoleGiver, Task> whenDone, params ulong[] rolesToIterate)
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

            public async Task GiveActiveClient(ulong userId)
            {
                log.Log($"Giving ActiveClient role to " + userId);
                await Task.CompletedTask;
            }

            public async Task GiveActiveHost(ulong userId)
            {
                log.Log($"Giving ActiveHost role to " + userId);
                await Task.CompletedTask;
            }

            public async Task GiveActiveP2pParticipant(ulong userId)
            {
                log.Log($"Giving ActiveP2p role to " + userId);
                await Task.CompletedTask;
            }

            public async Task RemoveActiveP2pParticipant(ulong userId)
            {
                log.Log($"Removing ActiveP2p role from " + userId);
                await Task.CompletedTask;
            }

            public async Task GiveAltruisticRole(ulong userId)
            {
                log.Log($"Giving Altruistic role to " + userId);
                await Task.CompletedTask;
            }

            public async Task RemoveActiveClient(ulong userId)
            {
                log.Log($"Removing ActiveClient role from " + userId);
                await Task.CompletedTask;
            }

            public async Task RemoveActiveHost(ulong userId)
            {
                log.Log($"Removing ActiveHost role from " + userId);
                await Task.CompletedTask;
            }
        }
    }
}
