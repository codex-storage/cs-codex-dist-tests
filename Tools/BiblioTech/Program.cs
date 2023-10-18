using ArgsUniform;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace BiblioTech
{
    public class Program
    {
        private DiscordSocketClient client = null!;

        public static Configuration Config { get; private set; } = null!;
        public static DeploymentFilesMonitor DeploymentFilesMonitor { get; } = new DeploymentFilesMonitor();

        public static Task Main(string[] args)
        {
            var uniformArgs = new ArgsUniform<Configuration>(PrintHelp, args);
            Config = uniformArgs.Parse();

            return new Program().MainAsync();
        }

        public async Task MainAsync()
        {
            Console.WriteLine("Starting Codex Discord Bot...");
            client = new DiscordSocketClient();
            client.Log += Log;

            var helloWorld = new HelloWorldCommand(client);

            //var cmdService = new CommandService();
            //var handler = new CommandHandler(client, cmdService);
            //await handler.InstallCommandsAsync();
            //Console.WriteLine("Command handler installed...");

            await client.LoginAsync(TokenType.Bot, Config.ApplicationToken);
            await client.StartAsync();
            Console.WriteLine("Running...");
            await Task.Delay(-1);
        }

        private static void PrintHelp()
        {
            Console.WriteLine("BiblioTech - Codex Discord Bot");
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
