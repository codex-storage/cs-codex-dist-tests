using ArgsUniform;
using BiblioTech.Commands;
using BiblioTech.Rewards;
using Discord;
using Discord.WebSocket;
using Logging;

namespace BiblioTech
{
    public class Program
    {
        private DiscordSocketClient client = null!;
        private CustomReplacement replacement = null!;

        public static Configuration Config { get; private set; } = null!;
        public static UserRepo UserRepo { get; } = new UserRepo();
        public static AdminChecker AdminChecker { get; private set; } = null!;
        public static IDiscordRoleDriver RoleDriver { get; set; } = null!;
        public static ILog Log { get; private set; } = null!;

        public static Task Main(string[] args)
        {
            Log = new ConsoleLog();

            var uniformArgs = new ArgsUniform<Configuration>(PrintHelp, args);
            Config = uniformArgs.Parse();

            Log = new LogSplitter(
                new FileLog(Path.Combine(Config.LogPath, "discordbot")),
                new ConsoleLog()
            );

            EnsurePath(Config.DataPath);
            EnsurePath(Config.UserDataPath);
            EnsurePath(Config.EndpointsPath);

            return new Program().MainAsync(args);
        }

        public async Task MainAsync(string[] args)
        {
            Log.Log("Starting Codex Discord Bot...");
            try
            {
                replacement = new CustomReplacement(Config);
                replacement.Load();
            }
            catch (Exception ex)
            {
                Log.Error("Failed to load logReplacements: " + ex);
                throw;
            }

            if (Config.DebugNoDiscord)
            {
                Log.Log("Debug option is set. Discord connection disabled!");
                RoleDriver = new LoggingRoleDriver(Log);
            }
            else
            {
                await StartDiscordBot();
            }

            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.ConfigureKestrel((context, options) =>
            {
                options.ListenAnyIP(Config.RewardApiPort);
            });
            builder.Services.AddControllers();
            var app = builder.Build();
            app.MapControllers();

            Log.Log("Running...");
            await app.RunAsync();
            await Task.Delay(-1);
        }

        private async Task StartDiscordBot()
        {
            client = new DiscordSocketClient();
            client.Log += ClientLog;

            var checker = new CodexCidChecker(Config, Log);
            var notifyCommand = new NotifyCommand();
            var associateCommand = new UserAssociateCommand(notifyCommand);
            var sprCommand = new SprCommand();
            var handler = new CommandHandler(Log, client, replacement,
                new GetBalanceCommand(associateCommand),
                new MintCommand(associateCommand),
                sprCommand,
                associateCommand,
                notifyCommand,
                new CheckCidCommand(checker),
                new AdminCommand(sprCommand, replacement)
            );

            await client.LoginAsync(TokenType.Bot, Config.ApplicationToken);
            await client.StartAsync();
            AdminChecker = new AdminChecker();
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
