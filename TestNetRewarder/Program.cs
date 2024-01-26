using ArgsUniform;
using CodexContractsPlugin.Marketplace;
using CodexContractsPlugin;
using GethPlugin;
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

                //Request[] GetStorageRequests(TimeRange timeRange);
                //EthAddress GetSlotHost(Request storageRequest, decimal slotIndex);
                //RequestState GetRequestState(Request request);
                //RequestFulfilledEventDTO[] GetRequestFulfilledEvents(TimeRange timeRange);
                //RequestCancelledEventDTO[] GetRequestCancelledEvents(TimeRange timeRange);
                //SlotFilledEventDTO[] GetSlotFilledEvents(TimeRange timeRange);
                //SlotFreedEventDTO[] GetSlotFreedEvents(TimeRange timeRange);



            }
            catch (Exception ex)
            {
                Log.Error("Exception processing time segment: " + ex);
            }

            await Task.Delay(1);
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
