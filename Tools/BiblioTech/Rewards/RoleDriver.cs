using Discord;
using Discord.WebSocket;
using DiscordRewards;
using Logging;
using Newtonsoft.Json;

namespace BiblioTech.Rewards
{
    public class RoleDriver : IDiscordRoleDriver
    {
        private readonly DiscordSocketClient client;
        private readonly ILog log;
        private readonly SocketTextChannel? rewardsChannel;
        private readonly ChainEventsSender eventsSender;
        private readonly RewardRepo repo = new RewardRepo();

        public RoleDriver(DiscordSocketClient client, ILog log, CustomReplacement replacement)
        {
            this.client = client;
            this.log = log;
            rewardsChannel = GetChannel(Program.Config.RewardsChannelId);
            eventsSender = new ChainEventsSender(log, replacement, GetChannel(Program.Config.ChainEventsChannelId));
        }

        public async Task GiveRewards(GiveRewardsCommand rewards)
        {
            log.Log($"Processing rewards command: '{JsonConvert.SerializeObject(rewards)}'");

            if (rewards.Rewards.Any())
            {
                await ProcessRewards(rewards);
            }

            await eventsSender.ProcessChainEvents(rewards.EventsOverview, rewards.Errors);
        }

        public async Task GiveAltruisticRole(IUser user)
        {
            var guild = GetGuild();
            var role = guild.Roles.SingleOrDefault(r => r.Id == Program.Config.AltruisticRoleId);
            if (role == null) return;

            var guildUser = guild.Users.SingleOrDefault(u => u.Id == user.Id);
            if (guildUser == null) return;

            await guildUser.AddRoleAsync(role);
        }

        private async Task ProcessRewards(GiveRewardsCommand rewards)
        {
            try
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
            catch (Exception ex)
            {
                log.Error("Failed to process rewards: " + ex);
            }
        }

        private SocketTextChannel? GetChannel(ulong id)
        {
            if (id == 0) return null;
            return GetGuild().TextChannels.SingleOrDefault(c => c.Id == id);
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
                    //var roleIds = string.Join(",", u.RoleIds.Select(r => r.ToString()).ToArray());
                    //log.Log($" > {u.Id}({u.DisplayName}) has [{roleIds}]");
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
                        log.Log($"No Reward is configured for id '{r.RewardId}'.");
                    }
                    else
                    {
                        var socketRole = guild.GetRole(r.RewardId);
                        if (socketRole == null)
                        {
                            log.Log($"Guild Role by id '{r.RewardId}' not found.");
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
                var userData =  Program.UserRepo.GetUserDataForAddress(new GethPlugin.EthAddress(address));
                if (userData != null) log.Log($"User '{userData.Name}' was looked up.");
                else log.Log($"Lookup for user was unsuccessful. EthAddress: '{address}'");
                return userData;
            }
            catch (Exception ex)
            {
                log.Error("Error during UserData lookup: " + ex);
                return null;
            }
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
}
