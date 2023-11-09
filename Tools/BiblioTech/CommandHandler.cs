using Discord.Net;
using Discord.WebSocket;
using Discord;
using Newtonsoft.Json;

namespace BiblioTech
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient client;
        private readonly BaseCommand[] commands;

        public CommandHandler(DiscordSocketClient client, params BaseCommand[] commands)
        {
            this.client = client;
            this.commands = commands;

            client.Ready += Client_Ready;
            client.SlashCommandExecuted += SlashCommandHandler;
        }

        private async Task Client_Ready()
        {
            var guild = client.Guilds.Single(g => g.Name == Program.Config.ServerName);
            Program.AdminChecker.SetGuild(guild);

            var builders = commands.Select(c =>
            {
                var builder = new SlashCommandBuilder()
                    .WithName(c.Name)
                    .WithDescription(c.Description);

                if (c.IsAdminCommand)
                {
                    builder.WithDefaultMemberPermissions(GuildPermission.Administrator);
                }

                foreach (var option in c.Options)
                {
                    builder.AddOption(option.Build());
                }

                return builder;
            });

            try
            {
                foreach (var builder in builders)
                {
                    await guild.CreateApplicationCommandAsync(builder.Build());
                }
            }
            catch (HttpException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                Console.WriteLine(json);
            }
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
