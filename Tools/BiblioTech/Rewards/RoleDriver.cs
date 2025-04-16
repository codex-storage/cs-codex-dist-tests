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
            var context = await OpenRoleModifyContext();
            var mapper = new RoleMapper(context);
            await action(mapper);
        }

        public async Task IterateRemoveActiveP2pParticipants(Func<IUser, bool> shouldRemove)
        {
            var context = await OpenRoleModifyContext();
            foreach (var user in context.Users)
            {
                if (user.RoleIds.Any(r => r == Program.Config.ActiveP2pParticipantRoleId))
                {
                    // This user has the role. Should it be removed?
                    if (shouldRemove(user))
                    {
                        await context.RemoveRole(user, Program.Config.ActiveP2pParticipantRoleId);
                    }
                }
            }
        }

        private async Task<RoleModifyContext> OpenRoleModifyContext()
        {
            var context = new RoleModifyContext(GetGuild(), userRepo, log, rewardsChannel);
            await context.Initialize();
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

        public async Task GiveActiveP2pParticipant(IUser user)
        {
            await context.GiveRole(user, Program.Config.ActiveP2pParticipantRoleId);
        }

        public async Task GiveAltruisticRole(IUser user)
        {
            await context.GiveRole(user, Program.Config.AltruisticRoleId);
        }
    }
}
