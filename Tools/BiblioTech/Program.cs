using ArgsUniform;
using BiblioTech.Commands;
using Discord;
using Discord.WebSocket;
using Logging;

namespace BiblioTech
{
    public class Program
    {
        private DiscordSocketClient client = null!;

        public static Configuration Config { get; private set; } = null!;
        public static UserRepo UserRepo { get; } = new UserRepo();
        public static AdminChecker AdminChecker { get; private set; } = null!;
        public static ILog Log { get; private set; } = null!;

        public static Task Main(string[] args)
        {
            var uniformArgs = new ArgsUniform<Configuration>(PrintHelp, args);
            Config = uniformArgs.Parse();

            Log = new LogSplitter(
                new FileLog(Path.Combine(Config.LogPath, "discordbot")),
                new ConsoleLog()
            );

            EnsurePath(Config.DataPath);
            EnsurePath(Config.UserDataPath);
            EnsurePath(Config.EndpointsPath);

            return new Program().MainAsync();
        }

        public async Task MainAsync()
        {
            Log.Log("Starting Codex Discord Bot...");
            client = new DiscordSocketClient();
            client.Log += ClientLog;

            var notifyCommand = new NotifyCommand();
            var associateCommand = new UserAssociateCommand(notifyCommand);
            var sprCommand = new SprCommand();
            var handler = new CommandHandler(client,
                new GetBalanceCommand(associateCommand), 
                new MintCommand(associateCommand),
                sprCommand,
                associateCommand,
                notifyCommand,
                new AdminCommand(sprCommand)
            );

            await client.LoginAsync(TokenType.Bot, Config.ApplicationToken);
            await client.StartAsync();

            AdminChecker = new AdminChecker();

            Log.Log("Running...");
            await Task.Delay(-1);
        }

        private static void PrintHelp()
        {
            Log.Log("BiblioTech - Codex Discord Bot");
        }

        private Task ClientLog(LogMessage msg)
        {
            Log.Log("DiscordClient: " + msg.ToString());
            return Task.CompletedTask;
        }

        private static void EnsurePath(string path)
        {
            if (Directory.Exists(path)) return;
            Directory.CreateDirectory(path);
        }
    }
}
