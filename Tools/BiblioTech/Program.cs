using ArgsUniform;
using BiblioTech.Commands;
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
        public static AdminChecker AdminChecker { get; } = new AdminChecker();

        public static Task Main(string[] args)
        {
            var uniformArgs = new ArgsUniform<Configuration>(PrintHelp, args);
            Config = uniformArgs.Parse();

            EnsurePath(Config.DataPath);
            EnsurePath(Config.UserDataPath);
            EnsurePath(Config.EndpointsPath);

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

            var ci = entryPoint.CreateInterface();

            var associateCommand = new UserAssociateCommand();
            var handler = new CommandHandler(client,
                new GetBalanceCommand(ci, associateCommand), 
                new MintCommand(ci, associateCommand),
                associateCommand,
                new AdminCommand()
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

        private static void EnsurePath(string path)
        {
            if (Directory.Exists(path)) return;
            Directory.CreateDirectory(path);
        }
    }
}
