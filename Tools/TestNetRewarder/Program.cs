using ArgsUniform;
using Logging;
using Utils;

namespace TestNetRewarder
{
    public class Program
    {
        public static CancellationToken CancellationToken;
        private static Configuration Config = null!;
        private static ILog Log = null!;
        private static BotClient BotClient = null!;
        private static Processor processor = null!;
        private static DateTime lastCheck = DateTime.MinValue;

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

            var connector = GethConnector.GethConnector.Initialize(Log);
            if (connector == null) throw new Exception("Invalid Geth information");

            BotClient = new BotClient(Config, Log);
            processor = new Processor(Config, BotClient, connector.CodexContracts, Log);

            EnsurePath(Config.DataPath);
            EnsurePath(Config.LogPath);

            return new Program().MainAsync();
        }

        public async Task MainAsync()
        {
            EnsureGethOnline();

            Log.Log("Starting TestNet Rewarder...");
            var segmenter = new TimeSegmenter(Log, Config.Interval, Config.HistoryStartUtc, processor);
            await processor.Initialize();
         
            while (!CancellationToken.IsCancellationRequested)
            {
                await EnsureBotOnline();
                await segmenter.ProcessNextSegment();
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
            var timeSince = start - lastCheck;
            if (timeSince.TotalSeconds < 30.0) return;

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

            lastCheck = start;
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
