using ArgsUniform;
using AutoClient;
using AutoClient.Modes;
using AutoClient.Modes.FolderStore;
using CodexClient;
using GethPlugin;
using Utils;

public class Program
{
    private readonly App app;
    private readonly List<IMode> modes = new List<IMode>();

    public Program(Configuration config)
    {
        app = new App(config);
    }

    public static async Task Main(string[] args)
    {
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, args) => cts.Cancel();

        var uniformArgs = new ArgsUniform<Configuration>(PrintHelp, args);
        var config = uniformArgs.Parse(true);

        if (config.NumConcurrentPurchases < 1)
        {
            throw new Exception("Number of concurrent purchases must be > 0");
        }

        var p = new Program(config);
        await p.Run();
    }

    public async Task Run()
    {
        await Task.CompletedTask;
        var codexNodes = CreateCodexWrappers();

        var i = 0;
        foreach (var cdx in codexNodes)
        {
            var mode = CreateMode();
            modes.Add(mode);

            mode.Start(cdx, i);
            i++;
        }

        app.Cts.Token.WaitHandle.WaitOne();

        foreach (var mode in modes) mode.Stop();
        modes.Clear();

        app.Log.Log("Done");
    }

    private IMode CreateMode()
    {
        if (!string.IsNullOrEmpty(app.Config.FolderToStore))
        {
            return CreateFolderStoreMode();
        }

        return new PurchasingMode(app);
    }

    private IMode CreateFolderStoreMode()
    {
        if (app.Config.ContractDurationMinutes - 1 < 5) throw new Exception("Contract duration config option not long enough!");

        return new FolderStoreMode(app, app.Config.FolderToStore, new PurchaseInfo(
            purchaseDurationTotal: TimeSpan.FromMinutes(app.Config.ContractDurationMinutes),
            purchaseDurationSafe: TimeSpan.FromMinutes(app.Config.ContractDurationMinutes - 120)
        ));
    }

    private CodexWrapper[] CreateCodexWrappers()
    {
        var endpointStrs = app.Config.CodexEndpoints.Split(";", StringSplitOptions.RemoveEmptyEntries);
        var result = new List<CodexWrapper>();

        foreach (var e in endpointStrs)
        {
            result.Add(CreateCodexWrapper(e));
        }

        return result.ToArray();
    }

    private readonly string LogLevel = "TRACE;info:discv5,providers,routingtable,manager,cache;warn:libp2p,multistream,switch,transport,tcptransport,semaphore,asyncstreamwrapper,lpstream,mplex,mplexchannel,noise,bufferstream,mplexcoder,secure,chronosstream,connection,websock,ws-session,muxedupgrade,upgrade,identify,contracts,clock,serde,json,serialization,JSONRPC-WS-CLIENT,JSONRPC-HTTP-CLIENT,codex,repostore";

    private CodexWrapper CreateCodexWrapper(string endpoint)
    {
        var splitIndex = endpoint.LastIndexOf(':');
        var host = endpoint.Substring(0, splitIndex);
        var port = Convert.ToInt32(endpoint.Substring(splitIndex + 1));

        var address = new Address(
            logName: $"cdx@{host}:{port}",
            host: host,
            port: port
        );

        var instance = CodexInstance.CreateFromApiEndpoint("[AutoClient]", address, EthAccountGenerator.GenerateNew());
        var node = app.CodexNodeFactory.CreateCodexNode(instance);

        node.SetLogLevel(LogLevel);

        return new CodexWrapper(app, node);
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Generates fake data and creates Codex storage contracts for it.");
    }
}
