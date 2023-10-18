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
        }

        private async Task Client_Ready()
        {
            // Let's build a guild command! We're going to need a guild so lets just put that in a variable.
            var guild = client.Guilds.Single(g => g.Name == "ThatBen's server");

            // Next, lets create our slash command builder. This is like the embed builder but for slash commands.
            var guildCommand = new SlashCommandBuilder();

            // Note: Names have to be all lowercase and match the regular expression ^[\w-]{3,32}$
            guildCommand.WithName("do-thing");

            // Descriptions can have a max length of 100.
            guildCommand.WithDescription("This command does the thing!");

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
