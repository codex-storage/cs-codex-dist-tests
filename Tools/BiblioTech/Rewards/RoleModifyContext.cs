using Discord.WebSocket;
using Discord;
using DiscordRewards;
using Nethereum.Model;
using Logging;

namespace BiblioTech.Rewards
{
    public class RoleModifyContext
    {
        private Dictionary<ulong, IGuildUser> users = new();
        private Dictionary<ulong, SocketRole> roles = new();
        private readonly SocketGuild guild;
        private readonly UserRepo userRepo;
        private readonly ILog log;
        private readonly SocketTextChannel? rewardsChannel;

        public RoleModifyContext(SocketGuild guild, UserRepo userRepo, ILog log, SocketTextChannel? rewardsChannel)
        {
            this.guild = guild;
            this.userRepo = userRepo;
            this.log = log;
            this.rewardsChannel = rewardsChannel;
        }

        public async Task Initialize()
        {
            this.users = await LoadAllUsers(guild);
            this.roles = LoadAllRoles(guild);
        }

        public IGuildUser[] Users => users.Values.ToArray();

        public async Task GiveRole(ulong userId, ulong roleId)
        {
            Log($"Giving role {roleId} to user {userId}");
            var role = GetRole(roleId);
            var guildUser = GetUser(userId);
            if (role == null) return;
            if (guildUser == null) return;

            await guildUser.AddRoleAsync(role);
            await Program.AdminChecker.SendInAdminChannel($"Added role '{role.Name}' for user <@{userId}>.");

            await SendNotification(guildUser, role);
        }

        public async Task RemoveRole(ulong userId, ulong roleId)
        {
            Log($"Removing role {roleId} from user {userId}");
            var role = GetRole(roleId);
            var guildUser = GetUser(userId);
            if (role == null) return;
            if (guildUser == null) return;

            await guildUser.RemoveRoleAsync(role);
            await Program.AdminChecker.SendInAdminChannel($"Removed role '{role.Name}' for user <@{userId}>.");
        }

        private SocketRole? GetRole(ulong roleId)
        {
            if (roles.ContainsKey(roleId)) return roles[roleId];
            return null;
        }

        private IGuildUser? GetUser(ulong userId)
        {
            if (users.ContainsKey(userId)) return users[userId];
            return null;
        }

        private void Log(string msg)
        {
            log.Log(msg);
        }

        private async Task<Dictionary<ulong, IGuildUser>> LoadAllUsers(SocketGuild guild)
        {
            log.Log("Loading all users..");
            var result = new Dictionary<ulong, IGuildUser>();
            var users = guild.GetUsersAsync();
            await foreach (var ulist in users)
            {
                foreach (var u in ulist)
                {
                    result.Add(u.Id, u);
                }
            }
            return result;
        }

        private Dictionary<ulong, SocketRole> LoadAllRoles(SocketGuild guild)
        {
            var result = new Dictionary<ulong, SocketRole>();
            var roles = guild.Roles.ToArray();
            foreach (var role in roles)
            {
                result.Add(role.Id, role);
            }
            return result;
        }

        private async Task SendNotification(IGuildUser user, SocketRole role)
        {
            try
            {
                var userData = userRepo.GetUser(user);
                if (userData == null) return;

                if (userData.NotificationsEnabled && rewardsChannel != null)
                {
                    var msg = $"<@{user.Id}> has received '{role.Name}'.";
                    await rewardsChannel.SendMessageAsync(msg);
                }
            }
            catch (Exception ex)
            {
                log.Error($"Failed to notify user '{user.DisplayName}' about role '{role.Name}': {ex}");
            }
        }
    }
}
