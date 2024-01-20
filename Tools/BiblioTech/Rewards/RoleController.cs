using Discord.WebSocket;
using Utils;

namespace BiblioTech.Rewards
{
    public class RoleController : IDiscordRoleController
    {
        private const string UsernameTag = "<USER>";
        private readonly DiscordSocketClient client;
        private readonly SocketTextChannel? rewardsChannel;

        private readonly RoleReward[] roleRewards = new[]
        {
            new RoleReward(1187039439558541498, $"Congratulations {UsernameTag}, you got the test-reward!")
        };

        public RoleController(DiscordSocketClient client)
        {
            this.client = client;

            if (!string.IsNullOrEmpty(Program.Config.RewardsChannelName))
            {
                rewardsChannel = GetGuild().TextChannels.SingleOrDefault(c => c.Name == Program.Config.RewardsChannelName);
            }
        }

        public void GiveRole(ulong roleId, UserData userData)
        {
            var reward = roleRewards.SingleOrDefault(r => r.RoleId == roleId);
            if (reward == null) { Program.Log.Log("no reward"); return; };

            var guild = GetGuild();

            var user = guild.GetUser(userData.DiscordId);
            if (user == null) { Program.Log.Log("no user"); return; };

            var role = guild.GetRole(roleId);
            if (role == null) { Program.Log.Log("no role"); return; };

            Program.Log.Log($"User has roles: {string.Join(",", user.Roles.Select(r => r.Name + "=" + r.Id))}");
            if (user.Roles.Any(r => r.Id == role.Id)) { Program.Log.Log("already has"); return; };

            GiveRole(user, role);
            SendNotification(reward, userData, user, role);
        }

        private void GiveRole(SocketGuildUser user, SocketRole role)
        {
            try
            {
                Program.Log.Log($"Giving role {role.Name}={role.Id} to user {user.DisplayName}");
                Time.Wait(user.AddRoleAsync(role));
            }
            catch (Exception ex)
            {
                Program.Log.Error($"Failed to give role '{role.Name}' to user '{user.DisplayName}': {ex}");
            }
        }

        private void SendNotification(RoleReward reward, UserData userData, SocketGuildUser user, SocketRole role)
        {
            try
            {
                if (userData.NotificationsEnabled && rewardsChannel != null)
                {
                    Time.Wait(rewardsChannel.SendMessageAsync(reward.Message.Replace(UsernameTag, user.DisplayName)));
                }
            }
            catch (Exception ex)
            {
                Program.Log.Error($"Failed to notify user '{user.DisplayName}' about role '{role.Name}': {ex}");
            }
        }

        private SocketGuild GetGuild()
        {
            return client.Guilds.Single(g => g.Name == Program.Config.ServerName);
        }
    }

    public class RoleReward
    {
        public RoleReward(ulong roleId, string message)
        {
            RoleId = roleId;
            Message = message;
        }

        public ulong RoleId { get; }
        public string Message { get; }
    }
}
