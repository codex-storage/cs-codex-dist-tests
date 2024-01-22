using Discord;
using Discord.WebSocket;

namespace BiblioTech.Rewards
{
    public class RoleController : IDiscordRoleController
    {
        public const string UsernameTag = "<USER>";
        private readonly DiscordSocketClient client;
        private readonly SocketTextChannel? rewardsChannel;
        private readonly RewardsRepo repo = new RewardsRepo();

        public RoleController(DiscordSocketClient client)
        {
            this.client = client;

            if (!string.IsNullOrEmpty(Program.Config.RewardsChannelName))
            {
                rewardsChannel = GetGuild().TextChannels.SingleOrDefault(c => c.Name == Program.Config.RewardsChannelName);
            }
        }

        public async Task GiveRewards(GiveRewardsCommand rewards)
        {
            var guild = GetGuild();
            // We load all role and user information first,
            // so we don't ask the server for the same info multiple times.
            var context = new RewardContext(
                await LoadAllUsers(guild),
                LookUpAllRoles(guild, rewards),
                rewardsChannel);

            await context.ProcessGiveRewardsCommand(rewards);
        }

        private async Task<Dictionary<ulong, IGuildUser>> LoadAllUsers(SocketGuild guild)
        {
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

        private Dictionary<ulong, RoleRewardConfig> LookUpAllRoles(SocketGuild guild, GiveRewardsCommand rewards)
        {
            var result = new Dictionary<ulong, RoleRewardConfig>();
            foreach (var r in rewards.Rewards)
            {
                var role = repo.Rewards.SingleOrDefault(rr => rr.RoleId == r.RewardId);
                if (role == null)
                {
                    Program.Log.Log($"No RoleReward is configured for reward with id '{r.RewardId}'.");
                }
                else
                {
                    if (role.SocketRole == null)
                    {
                        var socketRole = guild.GetRole(r.RewardId);
                        if (socketRole == null)
                        {
                            Program.Log.Log($"Guild Role by id '{r.RewardId}' not found.");
                        }
                        else
                        {
                            role.SocketRole = socketRole;
                        }
                    }
                    result.Add(role.RoleId, role);
                }
            }

            return result;
        }

        private SocketGuild GetGuild()
        {
            return client.Guilds.Single(g => g.Name == Program.Config.ServerName);
        }
    }

    public class RewardContext
    {
        private readonly Dictionary<ulong, IGuildUser> users;
        private readonly Dictionary<ulong, RoleRewardConfig> roles;
        private readonly SocketTextChannel? rewardsChannel;

        public RewardContext(Dictionary<ulong, IGuildUser> users, Dictionary<ulong, RoleRewardConfig> roles, SocketTextChannel? rewardsChannel)
        {
            this.users = users;
            this.roles = roles;
            this.rewardsChannel = rewardsChannel;
        }

        public async Task ProcessGiveRewardsCommand(GiveRewardsCommand rewards)
        {
            foreach (var rewardCommand in rewards.Rewards)
            {
                if (roles.ContainsKey(rewardCommand.RewardId))
                {
                    var role = roles[rewardCommand.RewardId];
                    await ProcessRewardCommand(role, rewardCommand);
                }
            }
        }

        private async Task ProcessRewardCommand(RoleRewardConfig role, RewardUsersCommand reward)
        {
            foreach (var user in reward.Users)
            {
                await GiveReward(role, user);
            }
        }

        private async Task GiveReward(RoleRewardConfig role, UserData user)
        {
            if (!users.ContainsKey(user.DiscordId))
            {
                Program.Log.Log($"User by id '{user.DiscordId}' not found.");
                return;
            }

            var guildUser = users[user.DiscordId];

            var alreadyHas = guildUser.RoleIds.ToArray();
            if (alreadyHas.Any(r => r == role.RoleId)) return;

            await GiveRole(guildUser, role.SocketRole!);
            await SendNotification(role, user, guildUser);
            await Task.Delay(1000);
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

        private async Task SendNotification(RoleRewardConfig reward, UserData userData, IGuildUser user)
        {
            try
            {
                if (userData.NotificationsEnabled && rewardsChannel != null)
                {
                    var msg = reward.Message.Replace(RoleController.UsernameTag, $"<@{user.Id}>");
                    await rewardsChannel.SendMessageAsync(msg);
                }
            }
            catch (Exception ex)
            {
                Program.Log.Error($"Failed to notify user '{user.DisplayName}' about role '{reward.SocketRole!.Name}': {ex}");
            }
        }
    }

    public class RoleRewardConfig
    {
        public RoleRewardConfig(ulong roleId, string message)
        {
            RoleId = roleId;
            Message = message;
        }

        public ulong RoleId { get; }
        public string Message { get; }
        public SocketRole? SocketRole { get; set; }
    }
}
