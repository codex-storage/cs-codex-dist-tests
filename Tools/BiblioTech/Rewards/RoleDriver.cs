using Discord;
using Discord.WebSocket;
using DiscordRewards;
using k8s.KubeConfigModels;
using Logging;
using Newtonsoft.Json;
using Utils;

namespace BiblioTech.Rewards
{
    public class RoleDriver : IDiscordRoleDriver
    {
        private readonly DiscordSocketClient client;
        private readonly UserRepo userRepo;
        private readonly ILog log;
        private readonly SocketTextChannel? rewardsChannel;

        public RoleDriver(DiscordSocketClient client, UserRepo userRepo, ILog log, SocketTextChannel? rewardsChannel)
        {
            this.client = client;
            this.userRepo = userRepo;
            this.log = log;
            this.rewardsChannel = rewardsChannel;
        }

        public async Task RunRoleGiver(Func<IRoleGiver, Task> action)
        {
            var context = OpenRoleModifyContext();
            var mapper = new RoleMapper(context);
            await action(mapper);
        }

        public async Task IterateUsersWithRoles(Func<IRoleGiver, IUser, ulong, Task> onUserWithRole, params ulong[] rolesToIterate)
        {
            await IterateUsersWithRoles(onUserWithRole, g => Task.CompletedTask, rolesToIterate);
        }

        public async Task IterateUsersWithRoles(Func<IRoleGiver, IUser, ulong, Task> onUserWithRole, Func<IRoleGiver, Task> whenDone, params ulong[] rolesToIterate)
        {
            var context = OpenRoleModifyContext();
            var mapper = new RoleMapper(context);
            foreach (var user in context.Users)
            {
                foreach (var role in rolesToIterate)
                {
                    if (user.RoleIds.Contains(role))
                    {
                        await onUserWithRole(mapper, user, role);
                    }
                }
            }
            await whenDone(mapper);
        }

        private RoleModifyContext OpenRoleModifyContext()
        {
            var context = new RoleModifyContext(GetGuild(), userRepo, log, rewardsChannel);
            context.Initialize();
            return context;
        }

        private SocketGuild GetGuild()
        {
            var guild = client.Guilds.SingleOrDefault(g => g.Id == Program.Config.ServerId);
            if (guild == null)
            {
                throw new Exception($"Unable to find guild by id: '{Program.Config.ServerId}'. " +
                    $"Known guilds: [{string.Join(",", client.Guilds.Select(g => g.Name + " (" + g.Id + ")"))}]");
            }
            return guild;
        }
    }

    public class RoleMapper : IRoleGiver
    {
        private readonly RoleModifyContext context;

        public RoleMapper(RoleModifyContext context)
        {
            this.context = context;
        }

        public async Task GiveActiveClient(ulong userId)
        {
            await context.GiveRole(userId, Program.Config.ActiveClientRoleId);
        }

        public async Task GiveActiveHost(ulong userId)
        {
            await context.GiveRole(userId, Program.Config.ActiveHostRoleId);
        }

        public async Task GiveActiveP2pParticipant(ulong userId)
        {
            await context.GiveRole(userId, Program.Config.ActiveP2pParticipantRoleId);
        }

        public async Task RemoveActiveP2pParticipant(ulong userId)
        {
            await context.RemoveRole(userId, Program.Config.ActiveP2pParticipantRoleId);
        }

        public async Task GiveAltruisticRole(ulong userId)
        {
            await context.GiveRole(userId, Program.Config.AltruisticRoleId);
        }

        public async Task RemoveActiveClient(ulong userId)
        {
            await context.RemoveRole(userId, Program.Config.ActiveClientRoleId);
        }

        public async Task RemoveActiveHost(ulong userId)
        {
            await context.RemoveRole(userId, Program.Config.ActiveHostRoleId);
        }
    }
}
