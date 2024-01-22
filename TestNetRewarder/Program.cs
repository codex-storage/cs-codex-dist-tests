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

            EnsurePath(Config.DataPath);
            EnsurePath(Config.LogPath);

            return new Program().MainAsync();
        }

        public async Task MainAsync()
        {
            Log.Log("Starting TestNet Rewarder...");
            var segmenter = new TimeSegmenter(Log, Config);
         
            while (!CancellationToken.IsCancellationRequested)
            {
                await segmenter.WaitForNextSegment(ProcessTimeSegment);
                await Task.Delay(1000, CancellationToken);
            }
        }

        private async Task ProcessTimeSegment(TimeRange range)
        {
            try
            {
                var connector = GethConnector.GethConnector.Initialize(Log);
                if (connector == null) return;

                var newRequests = connector.CodexContracts.GetStorageRequests(range);
                foreach (var request in newRequests)
                {
                    for (ulong i = 0; i < request.Ask.Slots; i++)
                    {
                        var host = connector.CodexContracts.GetSlotHost(request, i);
                    }
                }
                var newSlotsFilled  = connector.CodexContracts.GetSlotFilledEvents(range);
                var newSlotsFreed = connector.CodexContracts.GetSlotFreedEvents(range);
                
                // can we get them all?
            }
            catch (Exception ex)
            {
                Log.Error("Exception processing time segment: " + ex);
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
