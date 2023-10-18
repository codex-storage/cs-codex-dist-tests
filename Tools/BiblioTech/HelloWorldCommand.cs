using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace BiblioTech
{
    public class HelloWorldCommand
    {
        private readonly DiscordSocketClient client;

        public HelloWorldCommand(DiscordSocketClient client)
        {
            this.client = client;

            client.Ready += Client_Ready;
            client.SlashCommandExecuted += SlashCommandHandler;
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            var cheeseOption = command.Data.Options.SingleOrDefault(o => o.Name == "cheese");
            var numberOption = command.Data.Options.SingleOrDefault(o => o.Name == "numberofthings");
            await command.RespondAsync($"Dear {command.User.Username}, You executed {command.Data.Name} with cheese: {cheeseOption.Value} and number: {numberOption.Value}");
        }

        private async Task Client_Ready()
        {
            // Let's build a guild command! We're going to need a guild so lets just put that in a variable.
            var guild = client.Guilds.Single(g => g.Name == "ThatBen's server");

            // Next, lets create our slash command builder. This is like the embed builder but for slash commands.
            var guildCommand = new SlashCommandBuilder()
                .WithName("do-thing")
                .WithDescription("This command does the thing!")
                .AddOption("cheese", ApplicationCommandOptionType.Boolean, "whether you like cheese", isRequired: true)
                .AddOption("numberofthings", ApplicationCommandOptionType.Number, "count them please", isRequired: true)
                ;

            //// Let's do our global command
            //var globalCommand = new SlashCommandBuilder();
            //globalCommand.WithName("first-global-command");
            //globalCommand.WithDescription("This is my first global slash command");

            try
            {
                // Now that we have our builder, we can call the CreateApplicationCommandAsync method to make our slash command.
                await guild.CreateApplicationCommandAsync(guildCommand.Build());

                // With global commands we don't need the guild.
                //await client.CreateGlobalApplicationCommandAsync(globalCommand.Build());
                // Using the ready event is a simple implementation for the sake of the example. Suitable for testing and development.
                // For a production bot, it is recommended to only run the CreateGlobalApplicationCommandAsync() once for each command.
            }
            catch (ApplicationCommandException exception)
            {
                // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);

                // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
                Console.WriteLine(json);
            }
        }
    }
}
