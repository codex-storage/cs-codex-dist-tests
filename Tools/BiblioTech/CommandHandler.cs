using Discord.Net;
using Discord.WebSocket;
using Discord;
using Newtonsoft.Json;
using BiblioTech.Rewards;
using Logging;
using BiblioTech.CodexChecking;
using Nethereum.Model;

namespace BiblioTech
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient client;
        private readonly CustomReplacement replacement;
        private readonly ActiveP2pRoleRemover roleRemover;
        private readonly BaseCommand[] commands;
        private readonly ILog log;

        public CommandHandler(ILog log, DiscordSocketClient client, CustomReplacement replacement, ActiveP2pRoleRemover roleRemover, params BaseCommand[] commands)
        {
            this.client = client;
            this.replacement = replacement;
            this.roleRemover = roleRemover;
            this.commands = commands;
            this.log = log;
            client.Ready += Client_Ready;
            client.SlashCommandExecuted += SlashCommandHandler;
        }

        private async Task Client_Ready()
        {
            var guild = client.Guilds.Single(g => g.Id == Program.Config.ServerId);
            Program.AdminChecker.SetGuild(guild);
            log.Log($"Initializing for guild: '{guild.Name}'");

            var adminChannel = GetChannel(guild, Program.Config.AdminChannelId);
            if (adminChannel == null) throw new Exception("No admin message channel");
            var chainEventsChannel = GetChannel(guild, Program.Config.ChainEventsChannelId);
            var rewardsChannel = GetChannel(guild, Program.Config.RewardsChannelId);

            Program.AdminChecker.SetAdminChannel(adminChannel);
            Program.RoleDriver = new RoleDriver(client, Program.UserRepo, log, rewardsChannel);
            Program.ChainActivityHandler = new ChainActivityHandler(log, Program.UserRepo);
            Program.EventsSender = new ChainEventsSender(log, replacement, chainEventsChannel);

            var builders = commands.Select(c =>
            {
                var msg = $"Building command '{c.Name}' with options: ";
                var builder = new SlashCommandBuilder()
                    .WithName(c.Name)
                    .WithDescription(c.Description);

                foreach (var option in c.Options)
                {
                    msg += option.Name + " ";
                    builder.AddOption(option.Build());
                }

                log.Log(msg);
                return builder;
            });

            try
            {
                log.Log("Building application commands...");
                var commands = builders.Select(b => b.Build()).ToArray();

                log.Log("Submitting application commands...");
                var response = await guild.BulkOverwriteApplicationCommandAsync(commands);

                log.Log("Commands in response:");
                foreach (var cmd in response)
                {
                    log.Log($"{cmd.Name} ({cmd.Description}) [{DescribOptions(cmd.Options)}]");
                }

                roleRemover.Start();
            }
            catch (HttpException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                log.Error(json);
                throw;
            }
            Program.Dispatcher.Start();
            log.Log("Initialized.");
        }

        private SocketTextChannel? GetChannel(SocketGuild guild, ulong id)
        {
            if (id == 0) return null;
            return guild.TextChannels.SingleOrDefault(c => c.Id == id);
        }

        private string DescribOptions(IReadOnlyCollection<SocketApplicationCommandOption> options)
        {
            return string.Join(",", options.Select(DescribeOption).ToArray());
        }

        private string DescribeOption(SocketApplicationCommandOption option)
        {
            return $"({option.Name}[{DescribOptions(option.Options)}])";
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            foreach (var cmd in commands)
            {
                await cmd.SlashCommandHandler(command);
            }
        }
    }
}
