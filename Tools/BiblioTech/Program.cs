using ArgsUniform;
using BiblioTech.TokenCommands;
using Core;
using Discord;
using Discord.WebSocket;
using Logging;

namespace BiblioTech
{
    public class Program
    {
        private DiscordSocketClient client = null!;

        public static Configuration Config { get; private set; } = null!;
        public static DeploymentsFilesMonitor DeploymentFilesMonitor { get; } = new DeploymentsFilesMonitor();
        public static UserRepo UserRepo { get; } = new UserRepo();

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

            ProjectPlugin.Load<CodexPlugin.CodexPlugin>();
            ProjectPlugin.Load<GethPlugin.GethPlugin>();
            ProjectPlugin.Load<CodexContractsPlugin.CodexContractsPlugin>();

            var entryPoint = new EntryPoint(new ConsoleLog(), new KubernetesWorkflow.Configuration(
                kubeConfigFile: null,
                operationTimeout: TimeSpan.FromMinutes(5),
                retryDelay: TimeSpan.FromSeconds(10),
                kubernetesNamespace: "not-applicable"), "datafiles");

            var monitor = new DeploymentsFilesMonitor();

            var ci = entryPoint.CreateInterface();

            var handler = new CommandHandler(client,
                new GetBalanceCommand(monitor, ci), 
                new MintCommand(monitor, ci),
                new DeploymentsCommand(monitor)
            );

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
