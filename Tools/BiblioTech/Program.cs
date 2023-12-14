using ArgsUniform;
using BiblioTech.Commands;
using Discord;
using Discord.WebSocket;

namespace BiblioTech
{
    public class Program
    {
        private DiscordSocketClient client = null!;

        public static Configuration Config { get; private set; } = null!;
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

            var associateCommand = new UserAssociateCommand();
            var sprCommand = new SprCommand();
            var handler = new CommandHandler(client,
                new GetBalanceCommand(associateCommand), 
                new MintCommand(associateCommand),
                sprCommand,
                associateCommand,
                new AdminCommand(sprCommand)
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
