using Discord.WebSocket;
using Discord;
using DiscordRewards;

namespace BiblioTech.Rewards
{
    public class RewardContext
    {
        private readonly Dictionary<ulong, IGuildUser> users;
        private readonly Dictionary<ulong, RoleReward> roles;
        private readonly SocketTextChannel? rewardsChannel;

        public RewardContext(Dictionary<ulong, IGuildUser> users, Dictionary<ulong, RoleReward> roles, SocketTextChannel? rewardsChannel)
        {
            this.users = users;
            this.roles = roles;
            this.rewardsChannel = rewardsChannel;
        }

        public async Task ProcessGiveRewardsCommand(UserReward[] rewards)
        {
            foreach (var rewardCommand in rewards)
            {
                if (roles.ContainsKey(rewardCommand.RewardCommand.RewardId))
                {
                    var role = roles[rewardCommand.RewardCommand.RewardId];
                    await ProcessRewardCommand(role, rewardCommand);
                }
                else
                {
                    Program.Log.Error($"RoleID not found on guild: {rewardCommand.RewardCommand.RewardId}");
                }
            }
        }

        private async Task ProcessRewardCommand(RoleReward role, UserReward reward)
        {
            foreach (var user in reward.Users)
            {
                await GiveReward(role, user);
            }
        }

        private async Task GiveReward(RoleReward role, UserData user)
        {
            if (!users.ContainsKey(user.DiscordId))
            {
                Program.Log.Log($"User by id '{user.DiscordId}' not found.");
                return;
            }

            var guildUser = users[user.DiscordId];

            var alreadyHas = guildUser.RoleIds.ToArray();
            var logMessage = $"Giving reward '{role.SocketRole.Id}' to user '{user.DiscordId}'({user.Name})[" +
                $"alreadyHas:{string.Join(",", alreadyHas.Select(a => a.ToString()))}]: ";


            if (alreadyHas.Any(r => r == role.Reward.RoleId))
            {
                logMessage += "Already has role";
                Program.Log.Log(logMessage);
                return;
            }

            await GiveRole(guildUser, role.SocketRole);
            await SendNotification(role, user, guildUser);
            await Task.Delay(1000);
            logMessage += "Role given. Notification sent.";
            Program.Log.Log(logMessage);
        }

        private async Task GiveRole(IGuildUser user, SocketRole role)
        {
            try
            {
                Program.Log.Log($"Giving role {role.Name}={role.Id} to user {user.DisplayName}");
                await user.AddRoleAsync(role);
            }
            catch (Exception ex)
            {
                Program.Log.Error($"Failed to give role '{role.Name}' to user '{user.DisplayName}': {ex}");
            }
        }

        private async Task SendNotification(RoleReward reward, UserData userData, IGuildUser user)
        {
            try
            {
                if (userData.NotificationsEnabled && rewardsChannel != null)
                {
                    var msg = reward.Reward.Message.Replace(RewardConfig.UsernameTag, $"<@{user.Id}>");
                    await rewardsChannel.SendMessageAsync(msg);
                }
            }
            catch (Exception ex)
            {
                Program.Log.Error($"Failed to notify user '{user.DisplayName}' about role '{reward.SocketRole.Name}': {ex}");
            }
        }
    }
}
