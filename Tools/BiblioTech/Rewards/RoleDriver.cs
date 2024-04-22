using Discord;
using Discord.WebSocket;
using DiscordRewards;
using Newtonsoft.Json;

namespace BiblioTech.Rewards
{
    public class RoleDriver : IDiscordRoleDriver
    {
        private readonly DiscordSocketClient client;
        private readonly SocketTextChannel? rewardsChannel;
        private readonly SocketTextChannel? eventsChannel;
        private readonly RewardRepo repo = new RewardRepo();

        public RoleDriver(DiscordSocketClient client)
        {
            this.client = client;

            rewardsChannel = GetChannel(Program.Config.RewardsChannelName);
            eventsChannel = GetChannel(Program.Config.ChainEventsChannelName);
        }

        public async Task GiveRewards(GiveRewardsCommand rewards)
        {
            Program.Log.Log($"Processing rewards command: '{JsonConvert.SerializeObject(rewards)}'");

            if (rewards.Rewards.Any())
            {
                await ProcessRewards(rewards);
            }

            await ProcessChainEvents(rewards.EventsOverview);
        }

        private async Task ProcessRewards(GiveRewardsCommand rewards)
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

        private SocketTextChannel? GetChannel(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return GetGuild().TextChannels.SingleOrDefault(c => c.Name == name);
        }

        private async Task ProcessChainEvents(string[] eventsOverview)
        {
            if (eventsChannel == null || eventsOverview == null || !eventsOverview.Any()) return;
            await Task.Run(async () =>
            {
                foreach (var e in eventsOverview)
                {
                    if (!string.IsNullOrEmpty(e))
                    {
                        await eventsChannel.SendMessageAsync(e);
                        await Task.Delay(3000);
                    }
                }
            });
        }

        private async Task<Dictionary<ulong, IGuildUser>> LoadAllUsers(SocketGuild guild)
        {
            Program.Log.Log("Loading all users:");
            var result = new Dictionary<ulong, IGuildUser>();
            var users = guild.GetUsersAsync();
            await foreach (var ulist in users)
            {
                foreach (var u in ulist)
                {
                    result.Add(u.Id, u);
                    var roleIds = string.Join(",", u.RoleIds.Select(r => r.ToString()).ToArray());
                    Program.Log.Log($" > {u.Id}({u.DisplayName}) has [{roleIds}]");
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
                var userData =  Program.UserRepo.GetUserDataForAddress(new GethPlugin.EthAddress(address));
                if (userData != null) Program.Log.Log($"User '{userData.Name}' was looked up.");
                else Program.Log.Log($"Lookup for user was unsuccessful. EthAddress: '{address}'");
                return userData;
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
}
