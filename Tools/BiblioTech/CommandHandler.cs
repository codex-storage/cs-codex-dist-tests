﻿using Discord.Net;
using Discord.WebSocket;
using Discord;
using Newtonsoft.Json;
using BiblioTech.Rewards;
using Logging;

namespace BiblioTech
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient client;
        private readonly CustomReplacement replacement;
        private readonly BaseCommand[] commands;
        private readonly ILog log;

        public CommandHandler(ILog log, DiscordSocketClient client, CustomReplacement replacement, params BaseCommand[] commands)
        {
            this.client = client;
            this.replacement = replacement;
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

            var adminChannels = guild.TextChannels.Where(Program.AdminChecker.IsAdminChannel).ToArray();
            if (adminChannels == null || !adminChannels.Any()) throw new Exception("No admin message channel");
            Program.AdminChecker.SetAdminChannel(adminChannels.First());
            Program.RoleDriver = new RoleDriver(client, log, replacement);

            if (Program.Config.DeleteCommandsAtStartup > 0)
            {
                log.Log("Deleting all application commands...");
                await guild.DeleteApplicationCommandsAsync();
            }

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
                log.Log("Creating application commands...");
                foreach (var builder in builders)
                {
                    await guild.CreateApplicationCommandAsync(builder.Build());
                }
            }
            catch (HttpException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                log.Error(json);
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
