using Discord.Net;
using Discord.WebSocket;
using Discord;
using Newtonsoft.Json;
using Core;

namespace BiblioTech
{
    public class StatusCommand
    {
        private const string CommandName = "status";
        private readonly DiscordSocketClient client;
        private readonly EndpointsMonitor monitor;

        public StatusCommand(DiscordSocketClient client, EndpointsMonitor monitor)
        {
            this.client = client;
            this.monitor = monitor;

            client.Ready += Client_Ready;
            client.SlashCommandExecuted += SlashCommandHandler;
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (command.CommandName != CommandName) return;

            await command.RespondAsync(await monitor.GetReport());
        }

        private async Task Client_Ready()
        {
            var guild = client.Guilds.Single(g => g.Name == Program.Config.ServerName);

            var guildCommand = new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Display status of test net.");

            try
            {
                await guild.CreateApplicationCommandAsync(guildCommand.Build());
            }
            catch (HttpException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                Console.WriteLine(json);
            }
        }
    }
}
