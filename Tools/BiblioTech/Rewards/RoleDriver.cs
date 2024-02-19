using Discord;
using Discord.WebSocket;
using DiscordRewards;

namespace BiblioTech.Rewards
{
    public class RoleDriver : IDiscordRoleDriver
    {
        private readonly DiscordSocketClient client;
        private readonly SocketTextChannel? rewardsChannel;
        private readonly RewardRepo repo = new RewardRepo();

        public RoleDriver(DiscordSocketClient client)
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

            await context.ProcessGiveRewardsCommand(LookUpUsers(rewards));
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

        private Dictionary<ulong, RoleReward> LookUpAllRoles(SocketGuild guild, GiveRewardsCommand rewards)
        {
            var result = new Dictionary<ulong, RoleReward>();
            foreach (var r in rewards.Rewards)
            {
                if (!result.ContainsKey(r.RewardId))
                {
                    var rewardConfig = repo.Rewards.SingleOrDefault(rr => rr.RoleId == r.RewardId);
                    if (rewardConfig == null)
                    {
                        Program.Log.Log($"No Reward is configured for id '{r.RewardId}'.");
                    }
                    else
                    {
                        var socketRole = guild.GetRole(r.RewardId);
                        if (socketRole == null)
                        {
                            Program.Log.Log($"Guild Role by id '{r.RewardId}' not found.");
                        }
                        else
                        {
                            result.Add(r.RewardId, new RoleReward(socketRole, rewardConfig));
                        }
                    }
                }
            }

            return result;
        }

        private UserReward[] LookUpUsers(GiveRewardsCommand rewards)
        {
            return rewards.Rewards.Select(LookUpUserData).ToArray();
        }

        private UserReward LookUpUserData(RewardUsersCommand command)
        {
            return new UserReward(command,
                command.UserAddresses
                    .Select(LookUpUserDataForAddress)
                    .Where(d => d != null)
                    .Cast<UserData>()
                    .ToArray());
        }

        private UserData? LookUpUserDataForAddress(string address)
        {
            try
            {
                return Program.UserRepo.GetUserDataForAddress(new GethPlugin.EthAddress(address));
            }
            catch (Exception ex)
            {
                Program.Log.Error("Error during UserData lookup: " + ex);
                return null;
            }
        }

        private SocketGuild GetGuild()
        {
            var guild = client.Guilds.SingleOrDefault(g => g.Name == Program.Config.ServerName);
            if (guild == null)
            {
                throw new Exception($"Unable to find guild by name: '{Program.Config.ServerName}'. " +
                    $"Known guilds: [{string.Join(",", client.Guilds.Select(g => g.Name))}]");
            }
            return guild;
        }
    }

    public class RoleReward
    {
        public RoleReward(SocketRole socketRole, RewardConfig reward)
        {
            SocketRole = socketRole;
            Reward = reward;
        }

        public SocketRole SocketRole { get; }
        public RewardConfig Reward { get; }
    }

    public class UserReward
    {
        public UserReward(RewardUsersCommand rewardCommand, UserData[] users)
        {
            RewardCommand = rewardCommand;
            Users = users;
        }

        public RewardUsersCommand RewardCommand { get; }
        public UserData[] Users { get; }
    }

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
            if (alreadyHas.Any(r => r == role.Reward.RoleId)) return;

            await GiveRole(guildUser, role.SocketRole);
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
