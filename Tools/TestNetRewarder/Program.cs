using ArgsUniform;
using GethConnector;
using Logging;
using Utils;

namespace TestNetRewarder
{
    public class Program
    {
        public static Configuration Config { get; private set; } = null!;
        public static ILog Log { get; private set; } = null!;
        public static CancellationToken CancellationToken { get; private set; }
        public static BotClient BotClient { get; private set; } = null!;
        private static Processor processor = null!;

        public static Task Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            CancellationToken = cts.Token;
            Console.CancelKeyPress += (sender, args) => cts.Cancel();

            var uniformArgs = new ArgsUniform<Configuration>(PrintHelp, args);
            Config = uniformArgs.Parse(true);

            Log = new LogSplitter(
                new FileLog(Path.Combine(Config.LogPath, "testnetrewarder")),
                new ConsoleLog()
            );

            BotClient = new BotClient(Config, Log);
            processor = new Processor(Log);

            EnsurePath(Config.DataPath);
            EnsurePath(Config.LogPath);

            return new Program().MainAsync();
        }

        public async Task MainAsync()
        {
            EnsureGethOnline();

            Log.Log("Starting TestNet Rewarder...");
            var segmenter = new TimeSegmenter(Log, Config);
         
            while (!CancellationToken.IsCancellationRequested)
            {
                await EnsureBotOnline();
                await segmenter.WaitForNextSegment(processor.ProcessTimeSegment);
                await Task.Delay(100, CancellationToken);
            }
        }

        private static void EnsureGethOnline()
        {
            Log.Log("Checking Geth...");
            var gc = GethConnector.GethConnector.Initialize(Log);
            if (gc == null) throw new Exception("Geth input incorrect");

            var blockNumber = gc.GethNode.GetSyncedBlockNumber();
            if (blockNumber == null || blockNumber < 1) throw new Exception("Geth connection failed.");
            Log.Log("Geth OK. Block number: " + blockNumber);
        }

        private static async Task EnsureBotOnline()
        {
            var start = DateTime.UtcNow;
            while (! await BotClient.IsOnline() && !CancellationToken.IsCancellationRequested)
            {
                await Task.Delay(5000);

                var elapsed = DateTime.UtcNow - start;
                if (elapsed.TotalMinutes > 10)
                {
                    var msg = "Unable to connect to bot for " + Time.FormatDuration(elapsed);
                    Log.Error(msg);
                    throw new Exception(msg);
                }
            }
        }

        private static void PrintHelp()
        {
            Log.Log("TestNet Rewarder");
        }

        private static void EnsurePath(string path)
        {
            if (Directory.Exists(path)) return;
            Directory.CreateDirectory(path);
        }
    }
}
