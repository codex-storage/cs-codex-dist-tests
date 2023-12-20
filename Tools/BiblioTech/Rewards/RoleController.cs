using Discord.WebSocket;
using Utils;

namespace BiblioTech.Rewards
{
    public class RoleController : IDiscordRoleController
    {
        private const string UsernameTag = "<USER>";
        private readonly SocketGuild guild;
        private readonly SocketTextChannel? rewardsChannel;

        private readonly RoleReward[] roleRewards = new[]
        {
            new RoleReward(1187039439558541498, $"Congratulations {UsernameTag}, you got the test-reward!")
        };

        public RoleController(SocketGuild guild)
        {
            this.guild = guild;

            if (!string.IsNullOrEmpty(Program.Config.RewardsChannelName))
            {
                rewardsChannel = guild.TextChannels.SingleOrDefault(c => c.Name == Program.Config.RewardsChannelName);
            }
        }

        public void GiveRole(ulong roleId, UserData userData)
        {
            var reward = roleRewards.SingleOrDefault(r => r.RoleId == roleId);
            if (reward == null) return;

            var user = guild.Users.SingleOrDefault(u => u.Id == userData.DiscordId);
            if (user == null) return;

            var role = guild.Roles.SingleOrDefault(r => r.Id == roleId);
            if (role == null) return;


            GiveRole(user, role);
            SendNotification(reward, userData, user, role);
        }

        private void GiveRole(SocketGuildUser user, SocketRole role)
        {
            try
            {
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
